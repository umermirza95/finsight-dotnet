using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Finsight.Enums;
using Finsight.Interfaces;
using Finsight.Models;
using IBApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Finsight.Services
{
    public class IBKRBrokerService : IBrokerService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<IBKRBrokerService> _logger;
        private readonly IBKR.IBKRConnectionHandler _connectionHandler;

        public IBKRBrokerService(HttpClient httpClient, AppDbContext dbContext, IConfiguration configuration, ILogger<IBKRBrokerService> logger, IBKR.IBKRConnectionHandler connectionHandler)
        {
            _httpClient = httpClient;
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
            _connectionHandler = connectionHandler;
        }

        public bool IsConnected => _connectionHandler.Client.IsConnected();

        public void Connect()
        {
            var host = _configuration["IBKR:Host"] ?? "127.0.0.1";
            var port = int.Parse(_configuration["IBKR:Port"] ?? "7497");
            var clientId = int.Parse(_configuration["IBKR:ClientId"] ?? "1");
            _connectionHandler.Connect(host, port, clientId);
        }

        public void Disconnect()
        {
            _connectionHandler.Disconnect();
        }

        public async Task PlaceLimitOrderAsync(string ticker, TradeDirection direction, decimal limitPrice, decimal quantity)
        {
            if (!_connectionHandler.Client.IsConnected())
                throw new Exception("IBKR is not connected.");

            var config = await _dbContext.TradingConfigs.FirstOrDefaultAsync();
            if (config != null && config.LogsOnly)
            {
                return;
            }

            var contract = new Contract
            {
                Symbol = ticker,
                SecType = "STK",
                Exchange = "SMART",
                Currency = "USD"
            };

            var order = new Order
            {
                Action = direction == TradeDirection.BUY ? "BUY" : "SELL",
                OrderType = "LMT",
                TotalQuantity = (double)quantity,
                LmtPrice = (double)limitPrice,
                Tif = "GTC",
                OutsideRth = true
            };

            var orderId = _connectionHandler.GetNextOrderId();
            _connectionHandler.Client.placeOrder(orderId, contract, order);
            
            _logger.LogInformation($"Placed limit order {orderId} for {ticker} {direction} {quantity} @ {limitPrice}");
        }

        public async Task CancelOrderAsync(string brokerOrderId)
        {
            if (!_connectionHandler.Client.IsConnected())
                throw new Exception("IBKR is not connected.");

            var config = await _dbContext.TradingConfigs.FirstOrDefaultAsync();
            if (config != null && config.LogsOnly)
            {
               
                return;
            }

            if (int.TryParse(brokerOrderId, out var id))
            {
                _connectionHandler.Client.cancelOrder(id);
                _logger.LogInformation($"Cancelled order {id}");
            }
        }

        public async Task CancelAllOrdersAsync()
        {
            if (!_connectionHandler.Client.IsConnected())
                throw new Exception("IBKR is not connected.");

            var config = await _dbContext.TradingConfigs.FirstOrDefaultAsync();
            if (config != null && config.LogsOnly)
            {
                
                return;
            }

            _connectionHandler.Client.reqGlobalCancel();
            _logger.LogInformation("Requested global cancel of all open orders.");
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
            // await MatchClosedTradesAsync(userId); // Moved to FSTradingService
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
                if (parts.Length >= 9)
                {
                    var buySell = parts[5].Trim().ToUpper();
                    if (buySell == "BUY" || buySell == "SELL" || buySell == "B" || buySell == "S")
                    {
                        if (buySell == "B") buySell = "BUY";
                        if (buySell == "S") buySell = "SELL";

                        if (decimal.TryParse(parts[7], out var price) &&
                            decimal.TryParse(parts[6], out var qty) &&
                            decimal.TryParse(parts[8], out var comm))
                        {
                            records.Add(new IBKRTradeRecord
                            {
                                Symbol = parts[0].Trim(),
                                TradePrice = price,
                                Quantity = qty,
                                IBCommission = comm,
                                TradeDate = parts[4].Trim(),
                                BuySell = buySell,
                                IBOrderID = parts[1].Trim(),
                                DateTime = parts[3].Trim()
                            });
                        }
                        else
                        {
                            _logger.LogWarning("Found a Trade row but failed to parse decimals: Price={Price}, Qty={Qty}, Comm={Comm}", parts[7], parts[6], parts[8]);
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
