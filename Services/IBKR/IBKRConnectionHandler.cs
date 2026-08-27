using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Finsight.Interfaces;
using Finsight.Models;
using Finsight.Enums;

namespace Finsight.Services.IBKR
{
    public class IBKRConnectionHandler : IDisposable
    {
        private readonly ILogger<IBKRConnectionHandler> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMessagingService _messagingService;
        private readonly string _userId;
        private ClientWebSocket? _webSocket;
        private readonly HttpClient _httpClient;
        
        private bool _isConnected;
        public bool IsConnected => _isConnected && _webSocket?.State == WebSocketState.Open;

        private CancellationTokenSource _cts = new();

        public IBKRConnectionHandler(ILogger<IBKRConnectionHandler> logger, IServiceScopeFactory scopeFactory, IMessagingService messagingService, string userId)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _messagingService = messagingService;
            _userId = userId;

            // Optional: allow untrusted certs for local CP API gateway
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Finsight/1.0");
        }

        public async Task ConnectAsync(string host, int port)
        {
            if (IsConnected)
            {
                _logger.LogInformation("Already connected to IBKR REST API WebSocket.");
                return;
            }

            string baseUrl = $"https://{host}:{port}";
            _httpClient.BaseAddress = new Uri(baseUrl);

            try
            {
                _cts = new CancellationTokenSource();
                _webSocket = new ClientWebSocket();
                
                // Allow untrusted certs for local gateway websocket
                _webSocket.Options.RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true;

                // 1. Check if authenticated
                var authResponse = await _httpClient.GetAsync("/v1/api/iserver/auth/status");
                if (!authResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Failed to check IBKR CP API auth status. Status: {authResponse.StatusCode}");
                    throw new Exception($"Authentication failed or gateway is not reachable. Status: {authResponse.StatusCode}");
                }

                // 2. Set active account for the session
                var accountsResponse = await _httpClient.GetAsync("/v1/api/portfolio/accounts");
                if (accountsResponse.IsSuccessStatusCode)
                {
                    var accountsContent = await accountsResponse.Content.ReadAsStringAsync();
                    using var accountsDoc = System.Text.Json.JsonDocument.Parse(accountsContent);
                    if (accountsDoc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array && accountsDoc.RootElement.GetArrayLength() > 0)
                    {
                        var acc = accountsDoc.RootElement[0].GetProperty("accountId").GetString() ?? "U7630023";
                        var accPayload = new { acctId = acc };
                        var setAccReq = new HttpRequestMessage(HttpMethod.Post, "/v1/api/iserver/account");
                        setAccReq.Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(accPayload), Encoding.UTF8, "application/json");
                        await _httpClient.SendAsync(setAccReq);
                        _logger.LogInformation($"Selected active session account: {acc}");
                    }
                }

                // 2. Connect to WebSocket
                string wsUrl = $"wss://{host}:{port}/v1/api/ws";
                _logger.LogInformation($"Connecting to IBKR WebSocket at {wsUrl}");
                
                await _webSocket.ConnectAsync(new Uri(wsUrl), _cts.Token);
                _isConnected = true;
                
                await _messagingService.SendMessageAsync($"*IBKR Connection Established*: Connected to CP API at {baseUrl}");

                // 3. Start receive loop
                _ = ReceiveLoopAsync();
                
                // 4. Start keep-alive loop (ping/tickle)
                _ = KeepAliveLoopAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to IBKR CP API");
                _isConnected = false;
                throw;
            }
        }

        public void Disconnect()
        {
            if (_isConnected)
            {
                _logger.LogInformation("Disconnecting from IBKR CP API.");
                _cts.Cancel();
                
                if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                {
                    _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None).Wait(2000);
                }
                
                _webSocket?.Dispose();
            }
            _isConnected = false;
        }

        private async Task KeepAliveLoopAsync()
        {
            try
            {
                while (!_cts.IsCancellationRequested && IsConnected)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), _cts.Token);
                    
                    // Call tickle to maintain session
                    try
                    {
                        var content = new StringContent("{}", Encoding.UTF8, "application/json");
                        var response = await _httpClient.PostAsync("/v1/api/tickle", content, _cts.Token);
                        if (!response.IsSuccessStatusCode)
                        {
                            _logger.LogWarning("IBKR Tickle failed.");
                            Disconnect();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error during IBKR tickle");
                        Disconnect();
                    }
                }
            }
            catch (TaskCanceledException) { }
        }

        private async Task ReceiveLoopAsync()
        {
            var buffer = new byte[8192];

            try
            {
                while (_webSocket?.State == WebSocketState.Open && !_cts.IsCancellationRequested)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogWarning("IBKR WebSocket closed by server.");
                        Disconnect();
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        ProcessWebSocketMessage(message);
                    }
                }
            }
            catch (Exception ex) when (ex is not TaskCanceledException)
            {
                _logger.LogError(ex, "Error receiving IBKR WebSocket message.");
                Disconnect();
            }
        }

        private void ProcessWebSocketMessage(string message)
        {
            try
            {
                using var document = JsonDocument.Parse(message);
                var root = document.RootElement;

                if (root.TryGetProperty("topic", out var topicProp))
                {
                    string topic = topicProp.GetString() ?? "";

                    // Execution reports
                    if (topic == "trd" && root.TryGetProperty("args", out var args))
                    {
                        foreach (var arg in args.EnumerateArray())
                        {
                            HandleTradeExecutionEvent(arg);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process websocket message: {message}");
            }
        }

        private void HandleTradeExecutionEvent(JsonElement executionArgs)
        {
            // Sample execution payload:
            // { "executionId": "...", "symbol": "AAPL", "side": "BOT", "size": 100.0, "price": 140.23, "time": "20231010-14:22:30", "commission": 1.0, "conid": 265598 }
            
            try
            {
                string symbol = executionArgs.TryGetProperty("symbol", out var sym) ? sym.GetString() ?? "" : "";
                string side = executionArgs.TryGetProperty("side", out var s) ? s.GetString() ?? "" : "";
                decimal size = executionArgs.TryGetProperty("size", out var sz) ? sz.GetDecimal() : 0m;
                decimal price = executionArgs.TryGetProperty("price", out var p) ? p.GetDecimal() : 0m;
                decimal commission = executionArgs.TryGetProperty("commission", out var c) ? c.GetDecimal() : 0m;
                string executionId = executionArgs.TryGetProperty("executionId", out var eid) ? eid.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString();

                var direction = (side.Equals("BOT", StringComparison.OrdinalIgnoreCase) || side.Equals("BUY", StringComparison.OrdinalIgnoreCase)) 
                                ? TradeDirection.BUY : TradeDirection.SELL;

                Task.Run(async () => 
                {
                    using var scope = _scopeFactory.CreateScope();
                    var tradingService = scope.ServiceProvider.GetRequiredService<ITradingService>();
                    var messagingService = scope.ServiceProvider.GetRequiredService<IMessagingService>();
                    
                    await messagingService.SendMessageAsync($"*Order Executed (IBKR REST)*: {side} {size} shares of {symbol} at Avg Price ${price} ID: {executionId}");
                    
                    var now = DateTime.UtcNow;
                    var trade = new FSTrade
                    {
                        Id = Guid.NewGuid(),
                        FSUserId = _userId,
                        Ticker = symbol,
                        TradeDirection = direction,
                        TradePrice = price,
                        Quantity = size,
                        Date = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc),
                        ExternalId = executionId, // We use execution ID here as we might not have the parent Order ID easily accessible
                        Commission = commission
                    };
                    
                    await tradingService.HandleTradeExecutionAsync(trade);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse trade execution event.");
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _webSocket?.Dispose();
            _httpClient?.Dispose();
        }
    }
}
