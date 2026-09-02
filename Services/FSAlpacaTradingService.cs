using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Finsight.Commands;
using Finsight.DTOs;
using Finsight.Enums;
using Finsight.Interfaces;
using Finsight.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Finsight.Services
{
    public class FSAlpacaTradingService : IBrokerService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FSAlpacaTradingService> _logger;

        public FSAlpacaTradingService(HttpClient httpClient, AppDbContext dbContext, IConfiguration configuration, ILogger<FSAlpacaTradingService> logger)
        {
            _httpClient = httpClient;
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
        }

        private async Task<(string apiKey, string apiSecret, string baseUrl)> GetAlpacaConfigAsync(string userId)
        {
            var config = await _dbContext.TradingConfigs.FirstOrDefaultAsync(c => c.FSUserId == userId);
            if (config == null || string.IsNullOrEmpty(config.AlpacaApiKey) || string.IsNullOrEmpty(config.AlpacaApiSecret))
            {
                throw new Exception("Alpaca API credentials are not configured for this user.");
            }

            var baseUrl = _configuration["Alpaca:TradingBaseUrl"] ?? "https://paper-api.alpaca.markets";
            return (config.AlpacaApiKey, config.AlpacaApiSecret, baseUrl);
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string url, string apiKey, string apiSecret, object? payload = null)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("APCA-API-KEY-ID", apiKey);
            request.Headers.Add("APCA-API-SECRET-KEY", apiSecret);

            if (payload != null)
            {
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            }
            return request;
        }

        public bool IsConnected(string userId)
        {
            return true;
        }

        public Task ConnectAsync(string host, int port, int clientId, string userId)
        {
            return Task.CompletedTask;
        }

        public void Disconnect(string userId)
        {
        }

        public async Task PlaceLimitOrderAsync(string userId, string ticker, TradeDirection direction, decimal limitPrice, decimal quantity, string? account = null)
        {
            var (apiKey, apiSecret, baseUrl) = await GetAlpacaConfigAsync(userId);

            var payload = new
            {
                symbol = ticker,
                qty = (double)quantity,
                side = direction == TradeDirection.BUY ? "buy" : "sell",
                type = "limit",
                time_in_force = "gtc",
                limit_price = (double)limitPrice
            };

            var request = CreateRequest(HttpMethod.Post, $"{baseUrl}/v2/orders", apiKey, apiSecret, payload);
            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to place order in Alpaca: {content}");
                throw new Exception($"Alpaca PlaceOrder Error: {content}");
            }
        }

        public async Task<List<ActiveOrderDTO>> GetActiveOrdersAsync(string userId)
        {
            var (apiKey, apiSecret, baseUrl) = await GetAlpacaConfigAsync(userId);
            var request = CreateRequest(HttpMethod.Get, $"{baseUrl}/v2/orders?status=open", apiKey, apiSecret);
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode) return new List<ActiveOrderDTO>();

            var content = await response.Content.ReadAsStringAsync();
            var ordersList = new List<ActiveOrderDTO>();

            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var order in doc.RootElement.EnumerateArray())
                {
                    ordersList.Add(new ActiveOrderDTO
                    {
                        OrderId = order.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "",
                        ConId = 0,
                        Ticker = order.TryGetProperty("symbol", out var sym) ? sym.GetString() ?? "" : "",
                        Action = order.TryGetProperty("side", out var side) ? side.GetString()?.ToUpper() ?? "" : "",
                        Quantity = order.TryGetProperty("qty", out var q) ? GetDecimalSafe(q) : 0m,
                        LimitPrice = order.TryGetProperty("limit_price", out var p) ? GetDecimalSafe(p) : 0m
                    });
                }
            }
            return ordersList;
        }

        public async Task AdjustOrderPriceAsync(string userId, AdjustOrderPriceCommand command)
        {
            var (apiKey, apiSecret, baseUrl) = await GetAlpacaConfigAsync(userId);
            
            var payload = new
            {
                limit_price = (double)command.NewPrice,
                qty = (double)command.Quantity
            };

            var request = CreateRequest(HttpMethod.Patch, $"{baseUrl}/v2/orders/{command.OrderId}", apiKey, apiSecret, payload);
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to adjust order price in Alpaca: {content}");
            }
        }

        public async Task CancelOrderAsync(string userId, string permId)
        {
            var (apiKey, apiSecret, baseUrl) = await GetAlpacaConfigAsync(userId);
            var request = CreateRequest(HttpMethod.Delete, $"{baseUrl}/v2/orders/{permId}", apiKey, apiSecret);
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to cancel order in Alpaca: {content}");
            }
        }

        public async Task CancelAllOrdersAsync(string userId)
        {
            var (apiKey, apiSecret, baseUrl) = await GetAlpacaConfigAsync(userId);
            var request = CreateRequest(HttpMethod.Delete, $"{baseUrl}/v2/orders", apiKey, apiSecret);
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to cancel all orders in Alpaca: {content}");
            }
        }

        public async Task<List<FSTrade>> FetchTodayTradesAsync(string userId)
        {
            var (apiKey, apiSecret, baseUrl) = await GetAlpacaConfigAsync(userId);
            
            // Fetch closed orders from today
            var startOfDay = DateTime.UtcNow.Date.ToString("o");
            var request = CreateRequest(HttpMethod.Get, $"{baseUrl}/v2/orders?status=closed&after={startOfDay}", apiKey, apiSecret);
            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to fetch trades from Alpaca: {content}");
            }

            var fetchedTrades = new List<FSTrade>();
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var orderItem in doc.RootElement.EnumerateArray())
                {
                    string status = orderItem.TryGetProperty("status", out var st) ? st.GetString() ?? "" : "";
                    if (!status.Equals("filled", StringComparison.OrdinalIgnoreCase)) continue;

                    string side = orderItem.TryGetProperty("side", out var s) ? s.GetString() ?? "" : "";
                    var direction = side.Equals("buy", StringComparison.OrdinalIgnoreCase) ? TradeDirection.BUY : TradeDirection.SELL;
                    string ticker = orderItem.TryGetProperty("symbol", out var sym) ? sym.GetString() ?? "" : "";
                    
                    decimal tradePrice = orderItem.TryGetProperty("filled_avg_price", out var p) ? GetDecimalSafe(p) : 0m;
                    decimal quantity = orderItem.TryGetProperty("filled_qty", out var q) ? GetDecimalSafe(q) : 0m;
                    string externalId = orderItem.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";
                    
                    var filledAtStr = orderItem.TryGetProperty("filled_at", out var fa) ? fa.GetString() : null;
                    DateTime tradeDate = DateTime.UtcNow;
                    if (!string.IsNullOrEmpty(filledAtStr) && DateTime.TryParse(filledAtStr, out var parsedDate))
                    {
                        tradeDate = parsedDate;
                    }

                    if (tradeDate.Date < DateTime.UtcNow.Date) continue;

                    fetchedTrades.Add(new FSTrade
                    {
                        Id = Guid.NewGuid(),
                        FSUserId = userId,
                        Ticker = ticker,
                        TradePrice = tradePrice,
                        TradeDirection = direction,
                        Quantity = quantity,
                        Commission = 0m, // Alpaca is typically zero commission
                        Date = tradeDate,
                        ExternalId = externalId
                    });
                }
            }

            return fetchedTrades;
        }

        public async Task<decimal> GetUninvestedCashAsync(string userId)
        {
            var (apiKey, apiSecret, baseUrl) = await GetAlpacaConfigAsync(userId);
            var request = CreateRequest(HttpMethod.Get, $"{baseUrl}/v2/account", apiKey, apiSecret);
            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to fetch account info from Alpaca: {content}");
            }

            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("cash", out var cashProp))
            {
                return GetDecimalSafe(cashProp);
            }

            return 0m;
        }

        private decimal GetDecimalSafe(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Number) return element.GetDecimal();
            if (element.ValueKind == JsonValueKind.String && decimal.TryParse(element.GetString(), out decimal d)) return d;
            return 0m;
        }
    }
}
