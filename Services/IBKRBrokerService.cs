using System.Text;
using System.Text.Json;
using Finsight.Enums;
using Finsight.Interfaces;
using Finsight.Models;
using Microsoft.EntityFrameworkCore;
using Finsight.DTOs;
using Finsight.Services.IBKR;
using Finsight.Commands;

namespace Finsight.Services
{
    public class IBKRBrokerService : IBrokerService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<IBKRBrokerService> _logger;
        private readonly IIBKRConnectionManager _connectionManager;
        private readonly IMessagingService _messagingService;

        public IBKRBrokerService(HttpClient httpClient, AppDbContext dbContext, IConfiguration configuration, ILogger<IBKRBrokerService> logger, IIBKRConnectionManager connectionManager, IMessagingService messagingService)
        {
            _httpClient = httpClient;
            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "Finsight/1.0");
            }
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
            _connectionManager = connectionManager;
            _messagingService = messagingService;
        }

        private async Task<string> GetBaseUrlAsync(string userId)
        {
            var config = await _dbContext.TradingConfigs.FirstOrDefaultAsync(c => c.FSUserId == userId);
            if (config != null && !string.IsNullOrEmpty(config.ServerIp))
            {
                var host = config.ServerIp;
                var port = 7497;
                if (config.ServerIp.Contains(':'))
                {
                    var parts = config.ServerIp.Split(':');
                    host = parts[0];
                    if (int.TryParse(parts[1], out int parsedPort))
                        port = parsedPort;
                }
                return $"https://{host}:{port}";
            }
            return "https://localhost:5000"; // fallback
        }

        public bool IsConnected(string userId)
        {
            var handler = _connectionManager.GetHandler(userId);
            return handler != null && handler.IsConnected;
        }

        public async Task ConnectAsync(string host, int port, int clientId, string userId)
        {
            var handler = _connectionManager.GetOrCreateHandler(userId);
            await handler.ConnectAsync(host, port);
        }

        public void Disconnect(string userId)
        {
            _connectionManager.RemoveHandler(userId);
        }

        private async Task<int> GetConidAsync(string baseUrl, string ticker)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/api/iserver/secdef/search");
            request.Content = new StringContent(JsonSerializer.Serialize(new { symbol = ticker }), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
            {
                var first = doc.RootElement[0];
                if (first.TryGetProperty("conid", out var conidProp))
                {
                    if (conidProp.ValueKind == JsonValueKind.Number)
                        return conidProp.GetInt32();
                    if (conidProp.ValueKind == JsonValueKind.String && int.TryParse(conidProp.GetString(), out int parsedConid))
                        return parsedConid;
                }
            }
            throw new Exception($"Could not find conid for ticker {ticker}");
        }

        private async Task<string> HandleOrderResponseAsync(string baseUrl, string initialResponseContent)
        {
            string currentContent = initialResponseContent;
            int maxPrompts = 3;
            int promptCount = 0;

            while (promptCount < maxPrompts)
            {
                using var doc = JsonDocument.Parse(currentContent);
                bool promptFound = false;

                if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
                {
                    var first = doc.RootElement[0];
                    if (first.TryGetProperty("id", out var idProp))
                    {
                        string replyId = idProp.GetString() ?? "";
                        if (!string.IsNullOrEmpty(replyId))
                        {
                            promptFound = true;
                            promptCount++;
                            _logger.LogInformation($"Order requires confirmation (Prompt {promptCount}). Auto-replying to ID: {replyId}");
                            var replyPayload = new { confirmed = true };
                            var replyRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/api/iserver/reply/{replyId}");
                            replyRequest.Content = new StringContent(JsonSerializer.Serialize(replyPayload), Encoding.UTF8, "application/json");

                            var replyResponse = await _httpClient.SendAsync(replyRequest);
                            currentContent = await replyResponse.Content.ReadAsStringAsync();
                            _logger.LogInformation($"Confirmation reply response: {currentContent}");
                        }
                    }
                }

                if (!promptFound)
                {
                    break;
                }
            }

            return currentContent;
        }

        public async Task PlaceLimitOrderAsync(string userId, string ticker, TradeDirection direction, decimal limitPrice, decimal quantity, bool logsOnly, string? account = null)
        {
            if (!IsConnected(userId))
                throw new Exception("IBKR CP API is not connected.");

            await _messagingService.SendMessageAsync($"*Action Intent*: Placing {direction} limit order for {quantity} shares of {ticker} at ${limitPrice}.");

            if (logsOnly) return;

            string baseUrl = await GetBaseUrlAsync(userId);
            string acc = !string.IsNullOrEmpty(account) ? account : "U7630023";

            int conid = await GetConidAsync(baseUrl, ticker);
            _logger.LogInformation($"Fetched conid {conid} for ticker {ticker}");

            var orderPayload = new
            {
                orders = new[]
                {
                    new
                    {
                        conid = conid,
                        secType = $"{conid}:STK",
                        orderType = "LMT",
                        price = (double)limitPrice,
                        side = direction == TradeDirection.BUY ? "BUY" : "SELL",
                        quantity = (double)quantity,
                        tif = "GTC",
                        outsideRTH = true,
                        allOrNone = true
                    }
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/api/iserver/account/{acc}/orders");
            request.Content = new StringContent(JsonSerializer.Serialize(orderPayload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation($"Order placement response payload: {responseContent}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to place order: {responseContent}");
                throw new Exception($"Failed to place order: {responseContent}");
            }

            var finalContent = await HandleOrderResponseAsync(baseUrl, responseContent);

            string finalOrderId = "";
            bool orderConfirmed = false;

            using var doc = JsonDocument.Parse(finalContent);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                if (doc.RootElement.TryGetProperty("error", out var errProp))
                {
                    throw new Exception($"IBKR Order Error: {errProp.GetString()}");
                }
            }
            else if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
            {
                var first = doc.RootElement[0];
                if (first.TryGetProperty("order_id", out var orderIdProp))
                {
                    finalOrderId = orderIdProp.ValueKind == JsonValueKind.Number ? orderIdProp.GetInt32().ToString() : (orderIdProp.GetString() ?? "");
                    orderConfirmed = true;
                }
                else if (first.TryGetProperty("error", out var errProp))
                {
                    throw new Exception($"Order failed with IBKR error: {errProp.GetString()}");
                }
            }

            if (!orderConfirmed)
            {
                _logger.LogError($"Order placement failed to confirm. Last response: {finalContent}");
                throw new Exception($"Order placement failed to confirm. Last response: {finalContent}");
            }

            _logger.LogInformation($"Successfully placed limit order for {ticker} {direction} {quantity} @ {limitPrice}. Order ID: {finalOrderId}");
        }

        public async Task CancelAllOrdersAsync(string userId, bool logsOnly)
        {
            await _messagingService.SendMessageAsync("*Action Intent*: Canceling all open orders.");
            if (logsOnly) return;

            if (!IsConnected(userId))
                throw new Exception("IBKR is not connected.");

            string baseUrl = await GetBaseUrlAsync(userId);

            // Fetch all orders
            var activeOrders = await GetActiveOrdersAsync(userId);
            foreach (var order in activeOrders)
            {
                // Note: Getting account ID is typically required for canceling in CP API. Assuming default or config if needed.
                string acc = "U7630023"; // Simplification for now
                var request = new HttpRequestMessage(HttpMethod.Delete, $"{baseUrl}/v1/api/iserver/account/{acc}/order/{order.OrderId}");
                await _httpClient.SendAsync(request);
            }

            _logger.LogInformation("Requested global cancel of all open orders.");
        }

        public async Task<List<ActiveOrderDTO>> GetActiveOrdersAsync(string userId)
        {
            

            string baseUrl = await GetBaseUrlAsync(userId);

            // The 'filters' parameter expects specific statuses separated by commas
            var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/v1/api/iserver/account/orders?fFilters=Submitted");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new List<ActiveOrderDTO>();

            var content = await response.Content.ReadAsStringAsync();
            var ordersList = new List<ActiveOrderDTO>();
            using var doc = JsonDocument.Parse(content);
            JsonElement ordersArray;

            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                ordersArray = doc.RootElement;
            }
            else if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("orders", out var ordersProp) && ordersProp.ValueKind == JsonValueKind.Array)
            {
                ordersArray = ordersProp;
            }
            else
            {
                return ordersList;
            }

            foreach (var order in ordersArray.EnumerateArray())
            {
                string status = order.TryGetProperty("status", out var st) ? st.GetString() ?? "" : "";
                if (status.Equals("Submitted", StringComparison.OrdinalIgnoreCase))
                {
                    ordersList.Add(new ActiveOrderDTO
                    {
                        OrderId = order.TryGetProperty("orderId", out var oid) ? (oid.ValueKind == JsonValueKind.Number ? oid.GetInt32() : int.Parse(oid.GetString() ?? "0")) : 0,
                        ConId = order.TryGetProperty("conid", out var cid) ? (cid.ValueKind == JsonValueKind.Number ? cid.GetInt32() : int.Parse(cid.GetString() ?? "0")) : 0,
                        Ticker = order.TryGetProperty("ticker", out var t) ? t.GetString() ?? "" : "",
                        Action = order.TryGetProperty("side", out var s) ? s.GetString() ?? "" : "",
                        Quantity = order.TryGetProperty("remainingQuantity", out var rq) ? GetDecimalSafe(rq) : 0m,
                        LimitPrice = order.TryGetProperty("price", out var p) ? GetDecimalSafe(p) : 0m
                    });
                }


            }

            return ordersList;
        }

        public async Task AdjustOrderPriceAsync(string userId, AdjustOrderPriceCommand command)
        {
            string baseUrl = await GetBaseUrlAsync(userId);
            string acc = "U7630023"; // Need account ID, defaulting

            var payload = new
            {
                conid = command.ConId,
                orderType = "LMT",
                price = (double)command.NewPrice,
                quantity = (double)command.Quantity,
                side = command.Action,
                tif = "GTC",
                allOrNone = true
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/api/iserver/account/{acc}/order/{command.OrderId}");
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode && string.IsNullOrWhiteSpace(responseContent))
            {
                throw new Exception($"Failed to adjust order price. HTTP {(int)response.StatusCode}");
            }

            var finalContent = await HandleOrderResponseAsync(baseUrl, responseContent);

            bool orderConfirmed = false;

            using var doc = JsonDocument.Parse(finalContent);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                if (doc.RootElement.TryGetProperty("error", out var errProp))
                {
                    throw new Exception($"IBKR Error adjusting order price: {errProp.GetString()}");
                }
            }
            else if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
            {
                var first = doc.RootElement[0];
                if (first.TryGetProperty("order_id", out var orderIdProp))
                {
                    orderConfirmed = true;
                }
                else if (first.TryGetProperty("error", out var errProp))
                {
                    throw new Exception($"IBKR Error adjusting order price: {errProp.GetString()}");
                }
            }

            if (!orderConfirmed)
            {
                throw new Exception($"Failed to adjust order price: {finalContent}");
            }

            _logger.LogInformation($"Order price adjusted successfully. Order ID: {command.OrderId}");
        }

        public async Task CancelOrderAsync(string userId, int permId)
        {
            if (!IsConnected(userId))
                throw new Exception("IBKR is not connected.");

            string baseUrl = await GetBaseUrlAsync(userId);
            string acc = "U7630023";

            var request = new HttpRequestMessage(HttpMethod.Delete, $"{baseUrl}/v1/api/iserver/account/{acc}/order/{permId}");
            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to cancel order: {responseContent}");
            }

            _logger.LogInformation($"Requested cancel of order {permId}");
        }

        public async Task<List<FSTrade>> FetchTodayTradesAsync(string userId)
        {


            string baseUrl = await GetBaseUrlAsync(userId);
            var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/v1/api/iserver/account/trades");
            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to fetch trades. API returned: {content}");
            }
            var fetchedTrades = new List<FSTrade>();

            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                var requiredProps = new[] { "side", "symbol", "price", "size", "commission", "order_id", "trade_time" };

                foreach (var tradeItem in doc.RootElement.EnumerateArray())
                {
                    foreach (var prop in requiredProps)
                    {
                        if (!tradeItem.TryGetProperty(prop, out var element) || element.ValueKind == JsonValueKind.Null)
                        {
                            throw new Exception($"Trade item is missing required property: '{prop}'");
                        }
                    }

                    string side = tradeItem.GetProperty("side").GetString()!;
                    var direction = (side.Equals("B", StringComparison.OrdinalIgnoreCase) || side.Equals("BOT", StringComparison.OrdinalIgnoreCase) || side.Equals("BUY", StringComparison.OrdinalIgnoreCase))
                        ? TradeDirection.BUY : TradeDirection.SELL;

                    string ticker = tradeItem.GetProperty("symbol").GetString()!;
                    decimal tradePrice = GetDecimalSafe(tradeItem.GetProperty("price"));
                    decimal quantity = GetDecimalSafe(tradeItem.GetProperty("size"));
                    decimal commission = GetDecimalSafe(tradeItem.GetProperty("commission"));
                    var orderIdProp = tradeItem.GetProperty("order_id");
                    string externalId = orderIdProp.ValueKind == JsonValueKind.Number
                        ? orderIdProp.GetInt64().ToString()
                        : orderIdProp.GetString()!;

                    string timeStr = tradeItem.GetProperty("trade_time").GetString()!;
                    if (!DateTime.TryParseExact(timeStr, "yyyyMMdd-HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var tradeTimeUtc))
                    {
                        throw new Exception($"Could not parse trade_time '{timeStr}'.");
                    }

                    if (tradeTimeUtc.Date < DateTime.UtcNow.Date)
                    {
                        continue;
                    }

                    fetchedTrades.Add(new FSTrade
                    {
                        Id = Guid.NewGuid(),
                        FSUserId = userId,
                        Ticker = ticker,
                        TradePrice = tradePrice,
                        TradeDirection = direction,
                        Quantity = quantity,
                        Commission = commission,
                        Date = tradeTimeUtc,
                        ExternalId = externalId
                    });
                }
            }

            var mergedTrades = fetchedTrades
                .GroupBy(t => t.ExternalId)
                .Select(g =>
                {
                    var totalQuantity = g.Sum(t => t.Quantity);
                    var totalCommission = g.Sum(t => t.Commission);
                    var vwap = g.Sum(t => t.TradePrice * t.Quantity) / totalQuantity;

                    var firstTrade = g.First();
                    return new FSTrade
                    {
                        Id = Guid.NewGuid(),
                        FSUserId = firstTrade.FSUserId,
                        Ticker = firstTrade.Ticker,
                        TradePrice = vwap,
                        TradeDirection = firstTrade.TradeDirection,
                        Quantity = totalQuantity,
                        Commission = totalCommission,
                        Date = g.Max(t => t.Date),
                        ExternalId = firstTrade.ExternalId
                    };
                })
                .ToList();

            return mergedTrades;
        }

        public async Task<decimal> GetUninvestedCashAsync(string userId)
        {

            string baseUrl = await GetBaseUrlAsync(userId);
            string acc = "U7630023"; 
            var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/v1/api/portfolio/{acc}/ledger");
            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to fetch ledger. API returned: {content}");
            }

            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("BASE", out var baseElement))
            {
                if (baseElement.TryGetProperty("settledcash", out var cashbalanceElement))
                {
                    return GetDecimalSafe(cashbalanceElement);
                }
            }

            throw new Exception("Could not find cashbalance in the ledger response.");
        }

        private decimal GetDecimalSafe(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Number) return element.GetDecimal();
            if (element.ValueKind == JsonValueKind.String && decimal.TryParse(element.GetString(), out decimal d)) return d;
            return 0m;
        }
    }
}
