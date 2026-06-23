using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Finsight.Enums;
using Finsight.Interfaces;
using Finsight.Models;
using Finsight.Queries;
using Finsight.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Finsight.Services
{
    public class IBKRTradingService : ITradingService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<IBKRTradingService> _logger;

        public IBKRTradingService(HttpClient httpClient, AppDbContext dbContext, IConfiguration configuration, ILogger<IBKRTradingService> logger)
        {
            _httpClient = httpClient;
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task FetchMonthlyTradesAsync(string userId)
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning($"User {userId} not found.");
                return;
            }

            var token = user.IBKRToken;
            var queryId = user.IBKRQueryId;

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(queryId))
            {
                throw new Exception($"IBKR Token or QueryId is not configured for the user {userId}");
            }

            var requestUrl = $"https://ndcdyn.interactivebrokers.com/AccountManagement/FlexWebService/SendRequest?t={token}&q={queryId}&v=3";
            _logger.LogInformation("Requesting {Url}", requestUrl);
            var response = await _httpClient.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode();

            var xmlContent = await response.Content.ReadAsStringAsync();
            var doc = XDocument.Parse(xmlContent);
            var status = doc.Descendants("Status").FirstOrDefault()?.Value;

            if (status != "Success")
            {
                var error = doc.Descendants("ErrorMessage").FirstOrDefault()?.Value;
                throw new Exception($"IBKR Flex request failed: {error}");
            }

            var referenceCode = doc.Descendants("ReferenceCode").FirstOrDefault()?.Value;
            if (string.IsNullOrEmpty(referenceCode))
            {
                throw new Exception("Reference code not found in IBKR response.");
            }

            string? csvData = null;
            var statementUrl = $"https://ndcdyn.interactivebrokers.com/AccountManagement/FlexWebService/GetStatement?q={referenceCode}&t={token}&v=3";
            _logger.LogInformation("Polling for statement at {Url}", statementUrl);
            for (int i = 0; i < 12; i++)
            {
                await Task.Delay(5000); // wait 5 seconds
                var statementResponse = await _httpClient.GetAsync(statementUrl);

                if (statementResponse.IsSuccessStatusCode)
                {
                    csvData = await statementResponse.Content.ReadAsStringAsync();

                    // If it returns XML with an error about report not ready, it's not CSV yet.
                    if (csvData.StartsWith("<"))
                    {
                        var tryDoc = XDocument.Parse(csvData);
                        var tryStatus = tryDoc.Descendants("Status").FirstOrDefault()?.Value;
                        if (tryStatus == "Warn")
                        {
                            // "Statement generation in progress"
                            continue;
                        }
                    }

                    break;
                }
            }

            if (string.IsNullOrEmpty(csvData) || csvData.StartsWith("<"))
            {
                throw new Exception("Failed to retrieve CSV data from IBKR after polling.");
            }

            await ProcessCsvDataAsync(csvData, userId);
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
                    // Find the most recent buy that happened on or before the sell date
                    var matchedBuy = buys.FirstOrDefault(b => b.Date <= sell.Date);

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

        public async Task<OpenTradesResponse> GetOpenTradesAsync(string userId)
        {
            var trades = await _dbContext.FSTrades
                .Where(t => t.FSUserId == userId 
                && !_dbContext.FSClosedTrades.Any(c => c.OrderOpenId == t.ExternalId || c.OrderCloseId == t.ExternalId))
                .OrderByDescending(t => t.Date)
                .ToListAsync();

            var config = await _dbContext.TradingConfigs.FirstOrDefaultAsync(c => c.FSUserId == userId);

            decimal totalCapital = config?.TradingCapital ?? 0;
            decimal trancheSize = config?.TrancheSize ?? 0;
            
            decimal capitalUsed = trades.Sum(t => t.TradePrice * Math.Abs(t.Quantity));
            int availableTranches = 0;

            if (trancheSize > 0)
            {
                availableTranches = (int)Math.Floor((totalCapital - capitalUsed) / trancheSize);
                if (availableTranches < 0) availableTranches = 0;
            }

            return new OpenTradesResponse
            {
                Trades = trades,
                TotalCapital = totalCapital,
                CapitalUsed = capitalUsed,
                AvailableTranches = availableTranches
            };
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
                    BuyPrice = buyPrice,
                    SellPrice = sellPrice,
                    Commission = totalComm,
                    NetProfit = ((sellPrice - buyPrice) * quantity) - totalComm
                };
            }).ToList();
        }


        private async Task ProcessCsvDataAsync(string csvData, string userId)
        {
            var lines = csvData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            _logger.LogInformation("Processing {LineCount} lines of CSV data from IBKR.", lines.Length);
            var records = new List<IBKRTradeRecord>();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                var parts = SplitCsvLine(line);
                if (parts.Length >= 8)
                {
                    var buySell = parts[5].Trim().ToUpper();
                    if (buySell == "BUY" || buySell == "SELL" || buySell == "B" || buySell == "S")
                    {
                        if (buySell == "B") buySell = "BUY";
                        if (buySell == "S") buySell = "SELL";

                        if (decimal.TryParse(parts[1], out var price) &&
                            decimal.TryParse(parts[2], out var qty) &&
                            decimal.TryParse(parts[3], out var comm))
                        {
                            records.Add(new IBKRTradeRecord
                            {
                                Symbol = parts[0].Trim(),
                                TradePrice = price,
                                Quantity = qty,
                                IBCommission = comm,
                                TradeDate = parts[4].Trim(),
                                BuySell = buySell,
                                IBOrderID = parts[6].Trim(),
                                DateTime = parts[7].Trim()
                            });
                        }
                        else
                        {
                            _logger.LogWarning("Found a Trade row but failed to parse decimals: Price={Price}, Qty={Qty}, Comm={Comm}", parts[1], parts[2], parts[3]);
                        }
                    }
                }
            }

            var grouped = records.GroupBy(r => r.IBOrderID).ToList();
            var orderIds = grouped.Select(g => g.Key).ToList();
         

            var existingIds = await _dbContext.FSTrades
                .Where(t => orderIds.Contains(t.ExternalId))
                .Select(t => t.ExternalId)
                .ToListAsync();

            var newTrades = new List<FSTrade>();

            foreach (var group in grouped)
            {
                if (!existingIds.Contains(group.Key!))
                {
                    var first = group.First();
                    var direction = first.BuySell == "BUY" ? TradeDirection.BUY : TradeDirection.SELL;

                    var totalQty = group.Sum(x => Math.Abs(x.Quantity));
                    var totalComm = group.Sum(x => Math.Abs(x.IBCommission));
                    var vwap = totalQty > 0 ? group.Sum(x => x.TradePrice * Math.Abs(x.Quantity)) / totalQty : first.TradePrice;

                    DateTime.TryParseExact(first.DateTime, "yyyyMMdd;HHmmss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var parsedDate);

                    newTrades.Add(new FSTrade
                    {
                        Id = Guid.NewGuid(),
                        FSUserId = userId,
                        Ticker = first.Symbol,
                        TradePrice = vwap,
                        TradeDirection = direction,
                        Quantity = totalQty,
                        Commission = totalComm,
                        Date = parsedDate,
                        ExternalId = group.Key
                    });
                }
            }

            if (newTrades.Any())
            {
                _dbContext.FSTrades.AddRange(newTrades);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"Inserted {newTrades.Count} new trades from IBKR.");
            }
            else
            {
                _logger.LogInformation("No new trades to insert from IBKR.");
            }
        }

        private class IBKRTradeRecord
        {
            public string Symbol { get; set; } = string.Empty;
            public decimal TradePrice { get; set; }
            public decimal Quantity { get; set; }
            public decimal IBCommission { get; set; }
            public string? TradeDate { get; set; }
            public string? BuySell { get; set; }
            public string? IBOrderID { get; set; }
            public string? DateTime { get; set; }
        }

        private static string[] SplitCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            int startIndex = 0;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '\"')
                {
                    inQuotes = !inQuotes;
                }
                else if (line[i] == ',' && !inQuotes)
                {
                    result.Add(line.Substring(startIndex, i - startIndex).Trim('"', ' '));
                    startIndex = i + 1;
                }
            }
            result.Add(line.Substring(startIndex).Trim('"', ' '));
            return result.ToArray();
        }
    }
}
