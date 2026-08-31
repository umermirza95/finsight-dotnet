using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Finsight.Enums;
using Finsight.Interfaces;
using Finsight.Models;
using Finsight.Queries;
using Finsight.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Finsight.Services
{
    public class FSTradingService : ITradingService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<FSTradingService> _logger;
        private readonly IMarketDataService _marketDataService;
        private readonly IBrokerService _brokerService;
        private readonly ITransactionService _transactionService;
        private readonly IMessagingService _messagingService;


        public FSTradingService(AppDbContext dbContext, ILogger<FSTradingService> logger, IMarketDataService marketDataService, IBrokerService brokerService, ITransactionService transactionService, IMessagingService messagingService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _marketDataService = marketDataService;
            _brokerService = brokerService;
            _transactionService = transactionService;
            _messagingService = messagingService;
        }


        public async Task FetchTodayTradesAsync(string userId)
        {
            var fetchedTrades = await _brokerService.FetchTodayTradesAsync(userId);
            if (fetchedTrades == null || !fetchedTrades.Any())
                return;

            var distinctFetchedTrades = fetchedTrades.GroupBy(t => t.ExternalId).Select(g => g.First()).ToList();
            var orderIds = distinctFetchedTrades.Select(t => t.ExternalId).ToList();

            var existingTrades = await _dbContext.FSTrades
                .Where(t => t.FSUserId == userId && orderIds.Contains(t.ExternalId))
                .ToListAsync();

            var existingTradesDict = existingTrades.ToDictionary(t => t.ExternalId);

            var newTrades = new List<FSTrade>();
            var updatedTrades = new List<FSTrade>();

            foreach (var fetchedTrade in distinctFetchedTrades)
            {
                if (existingTradesDict.TryGetValue(fetchedTrade.ExternalId, out var existingTrade))
                {
                    if (existingTrade.Commission != fetchedTrade.Commission ||
                        existingTrade.TradePrice != fetchedTrade.TradePrice ||
                        existingTrade.Quantity != fetchedTrade.Quantity)
                    {
                        existingTrade.Commission = fetchedTrade.Commission;
                        existingTrade.TradePrice = fetchedTrade.TradePrice;
                        existingTrade.Quantity = fetchedTrade.Quantity;
                        existingTrade.Date = fetchedTrade.Date;
                        updatedTrades.Add(existingTrade);
                    }
                }
                else
                {
                    newTrades.Add(fetchedTrade);
                }
            }

            if (newTrades.Any())
            {
                _dbContext.FSTrades.AddRange(newTrades);
            }

            if (updatedTrades.Any())
            {
                var updatedTradeIds = updatedTrades.Select(t => t.ExternalId).ToList();
                
                var closedTradesToRecalculate = await _dbContext.FSClosedTrades
                    .Include(c => c.OpenTrade)
                    .Include(c => c.CloseTrade)
                    .Include(c => c.InsurancePayout)
                    .Where(c => c.FSUserId == userId && 
                               (updatedTradeIds.Contains(c.OrderOpenId) || updatedTradeIds.Contains(c.OrderCloseId)))
                    .ToListAsync();

                foreach (var closedTrade in closedTradesToRecalculate)
                {
                    closedTrade.RecalculateNetProfit();
                    if (closedTrade.InsurancePayout != null)
                    {
                        closedTrade.NetProfit += closedTrade.InsurancePayout.CoveredAmount;
                    }
                }
            }

            if (newTrades.Any() || updatedTrades.Any())
            {
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"Inserted {newTrades.Count} new trades, updated {updatedTrades.Count} existing trades for user {userId}.");
            }
            else
            {
                _logger.LogInformation($"No new trades to insert or update for today for user {userId}.");
            }
        }

        public async Task MatchClosedTradesAsync(string userId)
        {
            var unclosedTrades = await _dbContext.FSTrades
                .Where(t => t.FSUserId == userId
                && !_dbContext.FSClosedTrades.Any(c => c.OrderOpenId == t.ExternalId || c.OrderCloseId == t.ExternalId))
                .ToListAsync();

            var newClosedTrades = new List<FSClosedTrade>();
            var groupedByTicker = unclosedTrades.GroupBy(t => t.Ticker);

            foreach (var group in groupedByTicker)
            {
                // LIFO for Buys: Last In (most recent date) First Out
                var buys = group.Where(t => t.TradeDirection == TradeDirection.BUY).OrderByDescending(t => t.Date).ToList();
                // Process sells chronologically
                var sells = group.Where(t => t.TradeDirection == TradeDirection.SELL).OrderBy(t => t.Date).ToList();

                foreach (var sell in sells)
                {
                    // Find the most recent buy that happened on or before the sell date (LIFO)
                    var lifoBuy = buys.FirstOrDefault(b => b.Date <= sell.Date);

                    FSTrade? matchedBuy = null;

                    if (lifoBuy != null)
                    {
                        // Check if LIFO matching creates a profit
                        if (sell.TradePrice >= lifoBuy.TradePrice)
                        {
                            // Profit (or break-even): keep LIFO match
                            matchedBuy = lifoBuy;
                        }
                    }

                    if (matchedBuy != null)
                    {
                        var closedTrade = new FSClosedTrade
                        {
                            Id = Guid.NewGuid(),
                            FSUserId = userId,
                            OrderOpenId = matchedBuy.ExternalId,
                            OrderCloseId = sell.ExternalId
                        };
                        closedTrade.CalculateNetProfit(matchedBuy, sell);
                        newClosedTrades.Add(closedTrade);

                        // Remove matched buy so it's not matched again
                        buys.Remove(matchedBuy);
                    }
                }
            }

            if (newClosedTrades.Any())
            {
                _dbContext.FSClosedTrades.AddRange(newClosedTrades);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"Matched and created {newClosedTrades.Count} FSClosedTrades for user {userId}.");
            }
            else
            {
                _logger.LogInformation($"No new trade matches found for user {userId}.");
            }
        }

        public async Task<List<OpenTradeDTO>> GetOpenTradesAsync(string userId)
        {
            var trades = await _dbContext.FSTrades
                .Where(t => t.FSUserId == userId
                && t.Ticker != "EUR"
                && !_dbContext.FSClosedTrades.Any(c => c.OrderOpenId == t.ExternalId || c.OrderCloseId == t.ExternalId))
                .OrderByDescending(t => t.Date)
                .ToListAsync();

            var tickers = trades.Select(t => t.Ticker).Distinct().ToList();
            var prices = await _marketDataService.GetPricesAsync(tickers);

            var tradeDtos = trades.Select(trade =>
            {
                prices.TryGetValue(trade.Ticker, out var currentPrice);
                return OpenTradeDTO.FromEntity(trade, currentPrice);
            }).ToList();

            return tradeDtos;
        }

        public async Task<List<ClosedTradeResponse>> GetClosedTradesAsync(string userId, GetTradesQuery queryParams)
        {
            queryParams.ApplyDefaultDateRange();

            var query = _dbContext.FSClosedTrades
                .Include(c => c.OpenTrade)
                .Include(c => c.CloseTrade)
                .Include(c => c.InsurancePayout)
                .Where(c => c.FSUserId == userId);

            if (!string.IsNullOrEmpty(queryParams.Ticker))
            {
                query = query.Where(c => c.OpenTrade!.Ticker == queryParams.Ticker);
            }

            if (queryParams.StartDate.HasValue)
            {
                query = query.Where(c => c.CloseTrade!.Date >= queryParams.StartDate.Value);
            }

            if (queryParams.EndDate.HasValue)
            {
                query = query.Where(c => c.CloseTrade!.Date <= queryParams.EndDate.Value);
            }

            var closedTrades = await query.OrderByDescending(c => c.CloseTrade!.Date).ToListAsync();

            return closedTrades.Select(c =>
            {
                // Assuming all opening trades are BUY based on user request "Safe to assume there will never be Short trades"
                var buyPrice = c.OpenTrade!.TradePrice;
                var sellPrice = c.CloseTrade!.TradePrice;
                var quantity = c.OpenTrade.Quantity;
                var totalComm = c.OpenTrade.Commission + c.CloseTrade.Commission;

                return new ClosedTradeResponse
                {
                    ClosedTradeId = c.Id,
                    Ticker = c.OpenTrade.Ticker,
                    OpenDate = c.OpenTrade.Date,
                    CloseDate = c.CloseTrade.Date,
                    Quantity = quantity,
                    OpenTradeQuantity = c.OpenTrade.Quantity,
                    ClosedTradeQuantity = c.CloseTrade.Quantity,
                    BuyPrice = buyPrice,
                    SellPrice = sellPrice,
                    Commission = totalComm,
                    NetProfit = c.NetProfit, // Use the DB's NetProfit which includes any insurance payouts
                    InsuranceCoveredLoss = c.InsurancePayout?.CoveredAmount ?? 0
                };
            }).ToList();
        }

        public async Task HandleTradeExecutionAsync(FSTrade trade)
        {
            bool tradeExists = await _dbContext.FSTrades.AnyAsync(t => t.ExternalId == trade.ExternalId);
            if (tradeExists)
            {
                return;
            }

            try
            {
                _dbContext.FSTrades.Add(trade);
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"Error saving trade {trade.ExternalId} for user {trade.FSUserId}. Already processed.");
                return;
            }

            if (trade.TradeDirection == TradeDirection.SELL)
            {
                await MatchClosedTradesAsync(trade.FSUserId);
            }

            var config = await GetTradingConfigAsync(trade.FSUserId);
            if (config == null || !config.AutoTrade || config.SharesPerTranche == 0)
            {
                _logger.LogInformation("AutoTrade is disabled. Ignoring trade execution.");
                return;
            }

            await _messagingService.SendMessageAsync($"Trade executed: {trade.TradeDirection} {trade.Quantity} shares of {trade.Ticker} at ${trade.TradePrice}. Auto-trading is enabled, placing limit orders...");

            decimal shares = config.SharesPerTranche;
            decimal distancePercentage = config.DistancePerTranche / 100m;

            await _brokerService.CancelAllOrdersAsync(trade.FSUserId);

            var targetTicker = !string.IsNullOrWhiteSpace(config.Ticker) ? config.Ticker : trade.Ticker;

            if (trade.TradeDirection == TradeDirection.BUY)
            {
                decimal distance = trade.TradePrice * distancePercentage;
                decimal targetSellPrice = Math.Round(trade.TradePrice + distance, 2);

                await _brokerService.PlaceLimitOrderAsync(trade.FSUserId, targetTicker, TradeDirection.SELL, targetSellPrice, shares);

                decimal targetBuyPrice = Math.Round(trade.TradePrice - distance, 2);
                await _brokerService.PlaceLimitOrderAsync(trade.FSUserId, targetTicker, TradeDirection.BUY, targetBuyPrice, shares);
            }
            else // SELL
            {

                // Get most recent open buy trade from the database
                var mostRecentBuyTrade = await _dbContext.FSTrades
                    .Where(t => t.Ticker == targetTicker && t.TradeDirection == TradeDirection.BUY && !_dbContext.FSClosedTrades.Any(c => c.OrderOpenId == t.ExternalId))
                    .OrderByDescending(t => t.Date)
                    .FirstOrDefaultAsync();

                if (mostRecentBuyTrade == null)
                {
                    await _brokerService.PlaceLimitOrderAsync(trade.FSUserId, targetTicker, TradeDirection.BUY, Math.Round(trade.TradePrice, 2), shares);
                }
                else
                {
                    decimal distance = mostRecentBuyTrade.TradePrice * distancePercentage;

                    if (mostRecentBuyTrade.TradePrice - distance > trade.TradePrice)
                    {
                        await _brokerService.PlaceLimitOrderAsync(trade.FSUserId, targetTicker, TradeDirection.BUY, Math.Round(trade.TradePrice, 2), shares);
                    }
                    else
                    {
                        decimal targetBuyPrice = Math.Round(mostRecentBuyTrade.TradePrice - distance, 2);
                        decimal targetSellPrice = Math.Round(mostRecentBuyTrade.TradePrice + distance, 2);

                        await _brokerService.PlaceLimitOrderAsync(trade.FSUserId, targetTicker, TradeDirection.BUY, targetBuyPrice, shares);
                        await _brokerService.PlaceLimitOrderAsync(trade.FSUserId, targetTicker, TradeDirection.SELL, targetSellPrice, mostRecentBuyTrade.Quantity);
                    }

                }
            }

        }
        public async Task<FSTradingConfig?> GetTradingConfigAsync(string userId)
        {
            return await _dbContext.TradingConfigs.FirstOrDefaultAsync(c => c.FSUserId == userId);
        }

        public async Task<FSTradingConfig> UpdateTradingConfigAsync(string userId, Finsight.Commands.UpdateTradingConfigCommand dto)
        {
            var config = await _dbContext.TradingConfigs.FirstOrDefaultAsync(c => c.FSUserId == userId);

            if (config == null)
            {
                config = new FSTradingConfig
                {
                    Id = Guid.NewGuid(),
                    FSUserId = userId
                };
                _dbContext.TradingConfigs.Add(config);
            }

            if (dto.TradingCapital.HasValue) config.TradingCapital = dto.TradingCapital.Value;
            if (dto.TrancheSize.HasValue) config.TrancheSize = dto.TrancheSize.Value;
            if (dto.AutoTrade.HasValue) config.AutoTrade = dto.AutoTrade.Value;
            if (dto.SharesPerTranche.HasValue) config.SharesPerTranche = dto.SharesPerTranche.Value;
            if (dto.DistancePerTranche.HasValue) config.DistancePerTranche = dto.DistancePerTranche.Value;
            if (dto.ServerIp != null) config.ServerIp = dto.ServerIp;
            if (dto.Ticker != null) config.Ticker = dto.Ticker;

            await _dbContext.SaveChangesAsync();
            return config;
        }

        public async Task ManualMatchTradesAsync(string userId, Finsight.Commands.ManualMatchCommand command)
        {
            var buyTrade = await _dbContext.FSTrades.FirstOrDefaultAsync(t => t.ExternalId == command.BuyOrderId && t.FSUserId == userId);
            var sellTrade = await _dbContext.FSTrades.FirstOrDefaultAsync(t => t.ExternalId == command.SellOrderId && t.FSUserId == userId);

            if (buyTrade == null || sellTrade == null)
            {
                throw new InvalidOperationException("One or both trades not found.");
            }

            if (buyTrade.TradeDirection != TradeDirection.BUY || sellTrade.TradeDirection != TradeDirection.SELL)
            {
                throw new InvalidOperationException("Invalid trade directions. Must match a BUY and a SELL.");
            }

            if (buyTrade.Ticker != sellTrade.Ticker)
            {
                throw new InvalidOperationException("Trades must be of the same ticker.");
            }

            var alreadyMatched = await _dbContext.FSClosedTrades.AnyAsync(c =>
                (c.OrderOpenId == command.BuyOrderId || c.OrderCloseId == command.BuyOrderId) ||
                (c.OrderOpenId == command.SellOrderId || c.OrderCloseId == command.SellOrderId));

            if (alreadyMatched)
            {
                throw new InvalidOperationException("One or both trades are already matched.");
            }

            var closedTrade = new FSClosedTrade
            {
                Id = Guid.NewGuid(),
                FSUserId = userId,
                OrderOpenId = command.BuyOrderId,
                OrderCloseId = command.SellOrderId
            };
            closedTrade.CalculateNetProfit(buyTrade, sellTrade);

            if (closedTrade.NetProfit < 0)
            {
                var totalInsuranceFund = await _dbContext.FSProfitDistributions
                    .Where(d => d.FSUserId == userId && d.DistributionType == ProfitDistributionType.Insurance)
                    .SumAsync(d => d.Amount);

                var totalPayouts = await _dbContext.FSInsurancePayouts
                    .Where(p => p.FSUserId == userId)
                    .SumAsync(p => p.CoveredAmount);

                var availableInsurance = totalInsuranceFund - totalPayouts;

                if (availableInsurance > 0)
                {
                    var lossAmount = Math.Abs(closedTrade.NetProfit);
                    var coveredAmount = Math.Min(availableInsurance, lossAmount);

                    var payout = new FSInsurancePayout
                    {
                        Id = Guid.NewGuid(),
                        FSClosedTradeId = closedTrade.Id,
                        FSUserId = userId,
                        CoveredAmount = coveredAmount,
                        CreatedAt = DateTime.UtcNow
                    };

                    _dbContext.FSInsurancePayouts.Add(payout);
                    closedTrade.NetProfit += coveredAmount; // Adjust net profit
                }
            }

            _dbContext.FSClosedTrades.Add(closedTrade);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation($"Manually matched trade {command.BuyOrderId} with {command.SellOrderId} for user {userId}.");
        }

        public async Task MakeProfitDistributionAsync(string userId, Finsight.Commands.MakeProfitDistributionCommand command)
        {
            var availableBalance = await GetAvailableBalanceAsync(userId);

            if (command.Amount > availableBalance)
            {
                throw new InvalidOperationException("Insufficient total profit to distribute this amount.");
            }

            var distribution = new FSProfitDistribution
            {
                Id = Guid.NewGuid(),
                FSUserId = userId,
                Amount = command.Amount,
                DistributionType = command.Type,
                Date = DateTime.UtcNow
            };

            _dbContext.FSProfitDistributions.Add(distribution);

           

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"Recorded profit distribution of {command.Amount} for user {userId}.");
        }

        public async Task<decimal> GetAvailableBalanceAsync(string userId)
        {
            var balances = await _dbContext.Users
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    TotalNetProfit = _dbContext.FSClosedTrades.Where(c => c.FSUserId == userId).Sum(c => (decimal?)c.NetProfit) ?? 0,
                    TotalDistributed = _dbContext.FSProfitDistributions.Where(d => d.FSUserId == userId).Sum(d => (decimal?)d.Amount) ?? 0
                })
                .FirstOrDefaultAsync();

            if (balances == null) return 0;

            return balances.TotalNetProfit - balances.TotalDistributed;
        }

        public async Task<List<ProfitDistributionDTO>> GetProfitDistributionsAsync(string userId, GetProfitDistributionsQuery query)
        {
            var q = _dbContext.FSProfitDistributions.Where(d => d.FSUserId == userId);

            if (query.StartDate.HasValue)
                q = q.Where(d => d.Date >= query.StartDate.Value);

            if (query.EndDate.HasValue)
                q = q.Where(d => d.Date <= query.EndDate.Value);

            if (query.DistributionType.HasValue)
                q = q.Where(d => d.DistributionType == query.DistributionType.Value);

            var distributions = await q.OrderByDescending(d => d.Date).Take(10).ToListAsync();

            return distributions.Select(d => new ProfitDistributionDTO
            {
                Id = d.Id,
                Amount = d.Amount,
                DistributionType = d.DistributionType,
                Date = d.Date
            }).ToList();
        }

        public async Task<List<InsurancePayoutDTO>> GetInsurancePayoutsAsync(string userId, GetInsurancePayoutsQuery query)
        {
            var q = _dbContext.FSInsurancePayouts
                .Include(p => p.ClosedTrade)
                    .ThenInclude(ct => ct.OpenTrade)
                .Include(p => p.ClosedTrade)
                    .ThenInclude(ct => ct.CloseTrade)
                .Where(p => p.FSUserId == userId);

            if (query.StartDate.HasValue)
                q = q.Where(p => p.CreatedAt >= query.StartDate.Value);

            if (query.EndDate.HasValue)
                q = q.Where(p => p.CreatedAt <= query.EndDate.Value);

            var payouts = await q.OrderByDescending(p => p.CreatedAt).Take(10).ToListAsync();

            return payouts.Select(p => new InsurancePayoutDTO
            {
                Id = p.Id,
                CoveredAmount = p.CoveredAmount,
                ClosedTradeId = p.FSClosedTradeId,
                Date = p.CreatedAt,
                Ticker = p.ClosedTrade?.OpenTrade?.Ticker ?? "Unknown",
                BuyPrice = p.ClosedTrade?.OpenTrade?.TradeDirection == TradeDirection.BUY ? (p.ClosedTrade?.OpenTrade?.TradePrice ?? 0) : (p.ClosedTrade?.CloseTrade?.TradePrice ?? 0),
                SellPrice = p.ClosedTrade?.CloseTrade?.TradeDirection == TradeDirection.SELL ? (p.ClosedTrade?.CloseTrade?.TradePrice ?? 0) : (p.ClosedTrade?.OpenTrade?.TradePrice ?? 0)
            }).ToList();
        }

        public async Task<decimal> GetInsuranceBalanceAsync(string userId)
        {
            var balances = await _dbContext.Users
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    TotalInsuranceFund = _dbContext.FSProfitDistributions.Where(d => d.FSUserId == userId && d.DistributionType == ProfitDistributionType.Insurance).Sum(d => (decimal?)d.Amount) ?? 0,
                    TotalPayouts = _dbContext.FSInsurancePayouts.Where(p => p.FSUserId == userId).Sum(p => (decimal?)p.CoveredAmount) ?? 0
                })
                .FirstOrDefaultAsync();

            if (balances == null) return 0;

            return balances.TotalInsuranceFund - balances.TotalPayouts;
        }

        public async Task<decimal> GetTotalCapitalAsync(string userId)
        {
            var capitals = await _dbContext.Users
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    TotalCapitalInjected = _dbContext.FSInjectedCapitals.Where(c => c.FSUserId == userId).Sum(c => (decimal?)c.Amount) ?? 0,
                    TotalProfitReinvested = _dbContext.FSProfitDistributions.Where(d => d.FSUserId == userId && d.DistributionType == ProfitDistributionType.Reinvestment).Sum(d => (decimal?)d.Amount) ?? 0
                })
                .FirstOrDefaultAsync();

            if (capitals == null) return 0;

            return capitals.TotalCapitalInjected + capitals.TotalProfitReinvested;
        }

        public async Task<decimal> ReconcileBalanceWithBrokerAsync(string userId)
        {
            var uninvestedCash = await _brokerService.GetUninvestedCashAsync(userId);
            var profitForDistribution = await GetAvailableBalanceAsync(userId);
            var amountForInsurance = await GetInsuranceBalanceAsync(userId);
            var totalCapital = await GetTotalCapitalAsync(userId);

            var openBuyTrades = await _dbContext.FSTrades
                .Where(t => t.FSUserId == userId && t.TradeDirection == TradeDirection.BUY && !_dbContext.FSClosedTrades.Any(c => c.OrderOpenId == t.ExternalId))
                .ToListAsync();

            var totalCapitalDeployed = openBuyTrades.Sum(t => (t.Quantity * t.TradePrice) + t.Commission);

            var expectedCash = profitForDistribution + amountForInsurance + (totalCapital - totalCapitalDeployed);

            return uninvestedCash - expectedCash;
        }

        public async Task OpenLimitOrdersAsync(string userId)
        {
            var config = await GetTradingConfigAsync(userId);
            if (config == null || config.SharesPerTranche == 0)
            {
                throw new InvalidOperationException("Trading config not set or SharesPerTranche is 0.");
            }

            await _brokerService.CancelAllOrdersAsync(userId);
            
            var targetTicker = config.Ticker;
            if (string.IsNullOrWhiteSpace(targetTicker))
            {
                throw new InvalidOperationException("Ticker is not set in trading config.");
            }
            
            decimal shares = config.SharesPerTranche;
            decimal distancePercentage = config.DistancePerTranche / 100m;

            var mostRecentBuyTrade = await _dbContext.FSTrades
                .Where(t => t.FSUserId == userId && t.Ticker == targetTicker && t.TradeDirection == TradeDirection.BUY && !_dbContext.FSClosedTrades.Any(c => c.OrderOpenId == t.ExternalId))
                .OrderByDescending(t => t.Date)
                .FirstOrDefaultAsync();

            if (mostRecentBuyTrade == null)
            {
                throw new InvalidOperationException("No recent open BUY order found to calculate limit prices.");
            }

            decimal distance = mostRecentBuyTrade.TradePrice * distancePercentage;
            decimal targetBuyPrice = Math.Round(mostRecentBuyTrade.TradePrice - distance, 2);
            decimal targetSellPrice = Math.Round(mostRecentBuyTrade.TradePrice + distance, 2);

            await _brokerService.PlaceLimitOrderAsync(userId, targetTicker, TradeDirection.BUY, targetBuyPrice, shares);
            await _brokerService.PlaceLimitOrderAsync(userId, targetTicker, TradeDirection.SELL, targetSellPrice, mostRecentBuyTrade.Quantity);
        }
    }
}
