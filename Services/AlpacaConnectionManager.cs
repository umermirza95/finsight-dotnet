using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Alpaca.Markets;
using Finsight.Enums;
using Finsight.Interfaces;
using Finsight.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Finsight.Services
{
    public class AlpacaConnectionManager : BackgroundService
    {
        private readonly ILogger<AlpacaConnectionManager> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ConcurrentDictionary<string, IAlpacaStreamingClient> _clients;
        // Keep track of connected users to manage reconnections
        private readonly ConcurrentDictionary<string, FSTradingConfig> _configs;

        public AlpacaConnectionManager(ILogger<AlpacaConnectionManager> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _clients = new ConcurrentDictionary<string, IAlpacaStreamingClient>();
            _configs = new ConcurrentDictionary<string, FSTradingConfig>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AlpacaConnectionManager is starting.");

            await InitializeConnectionsAsync(stoppingToken);

            // Block until cancellation is requested
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        private async Task InitializeConnectionsAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var configs = await dbContext.TradingConfigs
                .Where(c => !string.IsNullOrEmpty(c.AlpacaApiKey) && !string.IsNullOrEmpty(c.AlpacaApiSecret))
                .ToListAsync(stoppingToken);

            foreach (var config in configs)
            {
                if (config.FSUserId == null) continue;
                
                _configs.TryAdd(config.FSUserId, config);
                await ConnectUserAsync(config.FSUserId, config.AlpacaApiKey!, config.AlpacaApiSecret!, stoppingToken);
            }
        }

        private async Task ConnectUserAsync(string userId, string apiKey, string apiSecret, CancellationToken stoppingToken)
        {
            if (_clients.ContainsKey(userId))
            {
                return;
            }

            try
            {
                var secretKey = new SecretKey(apiKey, apiSecret);
                var client = Alpaca.Markets.Environments.Live.GetAlpacaStreamingClient(secretKey);

                client.OnTradeUpdate += (tradeUpdate) => HandleTradeUpdateAsync(userId, tradeUpdate);
                client.OnError += (ex) => HandleError(userId, ex);
                
                var authStatus = await client.ConnectAndAuthenticateAsync(stoppingToken);

                if (authStatus == AuthStatus.Authorized)
                {
                    _clients.TryAdd(userId, client);
                    _logger.LogInformation($"Successfully connected Alpaca socket for user {userId}.");
                }
                else
                {
                    _logger.LogError($"Failed to authenticate Alpaca socket for user {userId}. Status: {authStatus}");
                    client.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception while connecting Alpaca socket for user {userId}. Retrying...");
                _ = ScheduleReconnectAsync(userId, apiKey, apiSecret, stoppingToken);
            }
        }

        private void HandleError(string userId, Exception ex)
        {
            _logger.LogError(ex, $"Alpaca streaming client error for user {userId}. Attempting to reconnect.");
            
            if (_clients.TryRemove(userId, out var client))
            {
                client.Dispose();
            }

            if (_configs.TryGetValue(userId, out var config))
            {
                _ = ScheduleReconnectAsync(userId, config.AlpacaApiKey!, config.AlpacaApiSecret!, CancellationToken.None);
            }
        }

        private async Task ScheduleReconnectAsync(string userId, string apiKey, string apiSecret, CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
                if (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation($"Attempting to reconnect Alpaca socket for user {userId}...");
                    await ConnectUserAsync(userId, apiKey, apiSecret, stoppingToken);
                }
            }
            catch (TaskCanceledException)
            {
                // Ignored
            }
        }

        private async void HandleTradeUpdateAsync(string userId, ITradeUpdate tradeUpdate)
        {
            try
            {
                if (tradeUpdate.Event != TradeEvent.Fill)
                {
                    return;
                }

                var order = tradeUpdate.Order;
                
                if (order.OrderStatus != OrderStatus.Filled)
                {
                    return;
                }
                
                // Throw exception if mandatory value is missing.
                var missingFields = new System.Collections.Generic.List<string>();

                if (order.Symbol == null) missingFields.Add(nameof(order.Symbol));
                if (tradeUpdate.Price == null) missingFields.Add(nameof(tradeUpdate.Price));
                if (order.Quantity == null) missingFields.Add(nameof(order.Quantity));
                if (order.OrderId == default) missingFields.Add(nameof(order.OrderId));
                if (tradeUpdate.TimestampUtc == null) missingFields.Add(nameof(tradeUpdate.TimestampUtc));

                if (missingFields.Any())
                {
                    throw new Exception($"Missing mandatory values in TradeUpdate: {string.Join(", ", missingFields)}");
                }

                var tradeDirection = order.OrderSide == OrderSide.Buy ? TradeDirection.BUY : TradeDirection.SELL;

                var fsTrade = new FSTrade
                {
                    Id = Guid.NewGuid(),
                    FSUserId = userId,
                    Ticker = order.Symbol!,
                    TradePrice = tradeUpdate.Price!.Value,
                    TradeDirection = tradeDirection,
                    Quantity = order.Quantity!.Value,
                    SharesLeft = order.Quantity!.Value,
                    Commission = 0m,
                    Date = tradeUpdate.TimestampUtc!.Value,
                    ExternalId = order.OrderId.ToString()
                };

                using var scope = _scopeFactory.CreateScope();
                var tradingService = scope.ServiceProvider.GetRequiredService<ITradingService>();

                await tradingService.HandleTradeExecutionAsync(fsTrade);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling trade update for user {userId}.");
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AlpacaConnectionManager is stopping.");

            foreach (var client in _clients.Values)
            {
                await client.DisconnectAsync(stoppingToken);
                client.Dispose();
            }

            _clients.Clear();

            await base.StopAsync(stoppingToken);
        }
    }
}
