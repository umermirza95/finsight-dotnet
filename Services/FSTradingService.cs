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
      

        public FSTradingService(AppDbContext dbContext, ILogger<FSTradingService> logger, IMarketDataService marketDataService, IBrokerService brokerService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _marketDataService = marketDataService;
            _brokerService = brokerService;
          
        }

        public async Task FetchMonthlyTradesAsync(string userId)
        {
            await _brokerService.FetchMonthlyTradesAsync(userId);
            await MatchClosedTradesAsync(userId);
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
                        else
                        {
                            // Loss: switch to FIFO (oldest buy order)
                            matchedBuy = buys.LastOrDefault(b => b.Date <= sell.Date);
                        }
                    }

                    if (matchedBuy != null)
                    {
                        newClosedTrades.Add(new FSClosedTrade
                        {
                            Id = Guid.NewGuid(),
                            FSUserId = userId,
                            OrderOpenId = matchedBuy.ExternalId,
                            OrderCloseId = sell.ExternalId
                        });

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
                    NetProfit = ((sellPrice - buyPrice) * quantity) - totalComm
                };
            }).ToList();
        }

        public async Task HandleTradeExecutionAsync(FSTrade trade)
        {
            _dbContext.FSTrades.Add(trade);
            await _dbContext.SaveChangesAsync();

            if (trade.TradeDirection == TradeDirection.SELL)
            {
                await MatchClosedTradesAsync(trade.FSUserId);
            }

            var config = await GetTradingConfigAsync();
            if (config == null || !config.AutoTrade || config.SharesPerTranche == 0)
            {
                _logger.LogInformation("AutoTrade is disabled. Ignoring trade execution.");
                return;
            }

            decimal shares = config.SharesPerTranche;
            decimal distance = config.DistancePerTranche;

            await _brokerService.CancelAllOrdersAsync();

            var targetTicker = !string.IsNullOrWhiteSpace(config.Ticker) ? config.Ticker : trade.Ticker;

            if (trade.TradeDirection == TradeDirection.BUY)
            {
                decimal targetSellPrice = trade.TradePrice + distance;
               
                await _brokerService.PlaceLimitOrderAsync(targetTicker, TradeDirection.SELL, targetSellPrice, shares, config.LogsOnly);
                
                decimal targetBuyPrice = trade.TradePrice - distance;
                await _brokerService.PlaceLimitOrderAsync(targetTicker, TradeDirection.BUY, targetBuyPrice, shares, config.LogsOnly);
            }
            else // SELL
            {
                decimal targetBuyPrice = trade.TradePrice - distance;
                await _brokerService.PlaceLimitOrderAsync(targetTicker, TradeDirection.BUY, targetBuyPrice, shares, config.LogsOnly);

                // Get most recent open buy trade from the database
                var mostRecentBuyTrade = await _dbContext.FSTrades
                    .Where(t => t.Ticker == targetTicker && t.TradeDirection == TradeDirection.BUY && !_dbContext.FSClosedTrades.Any(c => c.OrderOpenId == t.ExternalId))
                    .OrderByDescending(t => t.Date)
                    .FirstOrDefaultAsync();

                if (mostRecentBuyTrade == null)
                {
                    await _brokerService.PlaceLimitOrderAsync(targetTicker, TradeDirection.BUY, trade.TradePrice, shares, config.LogsOnly);
                }
                else
                {
                    if (mostRecentBuyTrade.TradePrice - distance > trade.TradePrice)
                    {
                        await _brokerService.PlaceLimitOrderAsync(targetTicker, TradeDirection.BUY, trade.TradePrice, shares, config.LogsOnly);
                    }
                    else
                    {
                        decimal nextSellPrice = mostRecentBuyTrade.TradePrice + distance;
                        await _brokerService.PlaceLimitOrderAsync(targetTicker, TradeDirection.SELL, nextSellPrice, shares, config.LogsOnly);
                    }
                }
            }
            
        }
        public async Task<FSTradingConfig?> GetTradingConfigAsync()
        {
            return await _dbContext.TradingConfigs.FirstOrDefaultAsync();
        }

        public async Task<FSTradingConfig> UpdateTradingConfigAsync(UpdateTradingConfigDTO dto)
        {
            var config = await _dbContext.TradingConfigs.FirstOrDefaultAsync();
            
            if (config == null)
            {
                config = new FSTradingConfig
                {
                    Id = Guid.NewGuid()
                };
                _dbContext.TradingConfigs.Add(config);
            }

            if (dto.TradingCapital.HasValue) config.TradingCapital = dto.TradingCapital.Value;
            if (dto.TrancheSize.HasValue) config.TrancheSize = dto.TrancheSize.Value;
            if (dto.AutoTrade.HasValue) config.AutoTrade = dto.AutoTrade.Value;
            if (dto.SharesPerTranche.HasValue) config.SharesPerTranche = dto.SharesPerTranche.Value;
            if (dto.DistancePerTranche.HasValue) config.DistancePerTranche = dto.DistancePerTranche.Value;
            if (dto.LogsOnly.HasValue) config.LogsOnly = dto.LogsOnly.Value;
            if (dto.DefaultUserId != null) config.DefaultUserId = dto.DefaultUserId;
            if (dto.Ticker != null) config.Ticker = dto.Ticker;

            await _dbContext.SaveChangesAsync();
            return config;
        }
    }
}
