using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Finsight.Enums;
using Finsight.Interfaces;
using Finsight.Models;
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
                _logger.LogWarning($"IBKR Token or QueryId is not configured for user {userId}.");
                return;
            }

            var requestUrl = $"https://gdcdyn.interactivebrokers.com/Universal/servlet/FlexStatementService.SendRequest?t={token}&q={queryId}&v=3";
            _logger.LogInformation("Requesting {Url}", requestUrl);
            var response = await _httpClient.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode();

            var xmlContent = await response.Content.ReadAsStringAsync();
            var doc = XDocument.Parse(xmlContent);
            var status = doc.Descendants("Status").FirstOrDefault()?.Value;
            
            if (status != "Success")
            {
                var error = doc.Descendants("ErrorMessage").FirstOrDefault()?.Value;
                _logger.LogError($"IBKR Flex request failed: {error}");
                return;
            }

            var referenceCode = doc.Descendants("ReferenceCode").FirstOrDefault()?.Value;
            if (string.IsNullOrEmpty(referenceCode))
            {
                _logger.LogError("Reference code not found in IBKR response.");
                return;
            }

            string? csvData = null;
            var statementUrl = $"https://gdcdyn.interactivebrokers.com/Universal/servlet/FlexStatementService.GetStatement?q={referenceCode}&t={token}&v=3";
            
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
                _logger.LogError("Failed to retrieve CSV data from IBKR after polling.");
                return;
            }

            await ProcessCsvDataAsync(csvData, userId);
        }

        private async Task ProcessCsvDataAsync(string csvData, string userId)
        {
            var lines = csvData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var records = new List<IBKRTradeRecord>();

            foreach (var line in lines)
            {
                var parts = line.Split(',');
                if (parts.Length >= 7)
                {
                    var buySell = parts[5].Trim().ToUpper();
                    if (buySell == "BUY" || buySell == "SELL")
                    {
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
                                IBOrderID = parts[6].Trim()
                            });
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
                if (existingIds.Contains(group.Key)) continue;

                var first = group.First();
                var direction = first.BuySell == "BUY" ? TradeDirection.BUY : TradeDirection.SELL;
                
                var totalQty = group.Sum(x => Math.Abs(x.Quantity));
                var totalComm = group.Sum(x => Math.Abs(x.IBCommission));
                var vwap = totalQty > 0 ? group.Sum(x => x.TradePrice * Math.Abs(x.Quantity)) / totalQty : first.TradePrice;

                DateOnly.TryParseExact(first.TradeDate, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var parsedDate);

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
            public string? Symbol { get; set; }
            public decimal TradePrice { get; set; }
            public decimal Quantity { get; set; }
            public decimal IBCommission { get; set; }
            public string? TradeDate { get; set; }
            public string? BuySell { get; set; }
            public string? IBOrderID { get; set; }
        }
    }
}
