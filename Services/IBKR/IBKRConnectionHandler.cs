using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
                
                try
                {
                    if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                    {
                        _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None).Wait(2000);
                    }
                    _webSocket?.Dispose();
                }
                catch (Exception)
                {
                    // Ignore exceptions during abrupt websocket closure
                }
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
                    try
                    {
                        var content = new StringContent("{}", Encoding.UTF8, "application/json");
                        var response = await _httpClient.PostAsync("/v1/api/tickle", content, _cts.Token);
                        if (!response.IsSuccessStatusCode)
                        {
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
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Disconnect();
                        break;
                    }
                    ProcessWebSocketMessage(message);
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
                _logger.LogInformation($"Received WebSocket message: {message}");
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
                    else if (topic == "sts" && root.TryGetProperty("args", out var argsSts))
                    {
                        if (argsSts.TryGetProperty("authenticated", out var authProp) && authProp.ValueKind == JsonValueKind.False)
                        {
                            _logger.LogWarning("IBKR WebSocket session is no longer authenticated. Disconnecting...");
                            Disconnect();
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
                var payload = executionArgs.Deserialize<TradeExecutionPayload>();

                if (payload == null) return;

                var direction = payload.Side.Equals("B", StringComparison.OrdinalIgnoreCase) ? TradeDirection.BUY : TradeDirection.SELL;

                Task.Run(async () => 
                {
                    using var scope = _scopeFactory.CreateScope();
                    var tradingService = scope.ServiceProvider.GetRequiredService<ITradingService>();
                
                    var now = DateTime.UtcNow;
                    var trade = new FSTrade
                    {
                        Id = Guid.NewGuid(),
                        FSUserId = _userId,
                        Ticker = payload.Symbol,
                        TradeDirection = direction,
                        TradePrice = payload.Price,
                        Quantity = payload.Size,
                        Date = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc),
                        ExternalId = payload.OrderId.ToString(),
                        Commission = payload.Commission
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

    public class TradeExecutionPayload
    {
        [JsonPropertyName("symbol")]
        [JsonRequired]
        public required string Symbol { get; set; }

        [JsonPropertyName("side")]
        [JsonRequired]
        public required string Side { get; set; }

        [JsonPropertyName("size")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        [JsonRequired]
        public decimal Size { get; set; }

        [JsonPropertyName("price")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        [JsonRequired]
        public decimal Price { get; set; }

        [JsonPropertyName("order_id")]
        [JsonRequired]
        public long OrderId { get; set; }

        [JsonPropertyName("commission")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal Commission { get; set; }

        
    }
}
