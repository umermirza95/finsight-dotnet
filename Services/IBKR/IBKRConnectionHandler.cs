using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
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
        private readonly CookieContainer _cookieContainer = new CookieContainer();
        
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
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
                CookieContainer = _cookieContainer,
                UseCookies = true
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
            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri(baseUrl);
            }

            try
            {
                _cts = new CancellationTokenSource();
                _webSocket = new ClientWebSocket();
                
                // Allow untrusted certs for local gateway websocket
                _webSocket.Options.RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true;
                _webSocket.Options.Cookies = _cookieContainer;

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

                // Subscribe to real-time trade updates
                var subPayload = "str+{\"realtimeUpdatesOnly\":true}";
                var subBytes = Encoding.UTF8.GetBytes(subPayload);
                await _webSocket.SendAsync(new ArraySegment<byte>(subBytes), WebSocketMessageType.Text, true, _cts.Token);
                _logger.LogInformation("Sent subscription for real-time trades on WebSocket.");

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
                        _logger.LogInformation("IBKR Tickle successful.");
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
                    if (topic == "str" && root.TryGetProperty("args", out var args))
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
            _logger.LogInformation($"Trade execution payload: {executionArgs.GetRawText()}");
            try
            {
                var requiredProps = new[] { "symbol", "side", "size", "price" };
                foreach (var prop in requiredProps)
                {
                    if (!executionArgs.TryGetProperty(prop, out var element) || element.ValueKind == JsonValueKind.Null)
                    {
                        throw new Exception($"Trade execution is missing required property: '{prop}'");
                    }
                }

                string symbol = executionArgs.GetProperty("symbol").GetString() ?? throw new Exception("symbol cannot be null");
                string side = executionArgs.GetProperty("side").GetString() ?? throw new Exception("side cannot be null");
                
                var sizeProp = executionArgs.GetProperty("size");
                decimal size = sizeProp.ValueKind == JsonValueKind.Number ? sizeProp.GetDecimal() : decimal.Parse(sizeProp.GetString()!);
                
                var priceProp = executionArgs.GetProperty("price");
                decimal price = priceProp.ValueKind == JsonValueKind.Number ? priceProp.GetDecimal() : decimal.Parse(priceProp.GetString()!);
                
                decimal commission = 0m;
                if (executionArgs.TryGetProperty("commission", out var cProp) && cProp.ValueKind != JsonValueKind.Null)
                {
                    commission = cProp.ValueKind == JsonValueKind.Number ? cProp.GetDecimal() : decimal.Parse(cProp.GetString()!);
                }
                
                string executionId = "";
                if (executionArgs.TryGetProperty("executionId", out var execIdProp) && execIdProp.ValueKind != JsonValueKind.Null)
                    executionId = execIdProp.GetString() ?? "";
                else if (executionArgs.TryGetProperty("execution_id", out var execIdSnakeProp) && execIdSnakeProp.ValueKind != JsonValueKind.Null)
                    executionId = execIdSnakeProp.GetString() ?? "";
                else
                    throw new Exception("executionId or execution_id cannot be null");

                string orderId = "";
                if (executionArgs.TryGetProperty("order_id", out var oidProp) && oidProp.ValueKind != JsonValueKind.Null)
                {
                    orderId = oidProp.ValueKind == JsonValueKind.Number ? oidProp.GetInt64().ToString() : (oidProp.GetString() ?? "");
                }
                else if (executionArgs.TryGetProperty("orderId", out var oidPropCamel) && oidPropCamel.ValueKind != JsonValueKind.Null)
                {
                    orderId = oidPropCamel.ValueKind == JsonValueKind.Number ? oidPropCamel.GetInt64().ToString() : (oidPropCamel.GetString() ?? "");
                }

                string externalIdToUse = !string.IsNullOrEmpty(orderId) ? orderId : executionId;

                var direction = (side.Equals("BOT", StringComparison.OrdinalIgnoreCase) || side.Equals("BUY", StringComparison.OrdinalIgnoreCase)) 
                                ? TradeDirection.BUY : TradeDirection.SELL;

                Task.Run(async () => 
                {
                    using var scope = _scopeFactory.CreateScope();
                    var tradingService = scope.ServiceProvider.GetRequiredService<ITradingService>();
                    var messagingService = scope.ServiceProvider.GetRequiredService<IMessagingService>();
                    
                    await messagingService.SendMessageAsync($"*Order Executed (IBKR REST)*: Raw Payload: {executionArgs.GetRawText()}");
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
                        ExternalId = externalIdToUse,
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
