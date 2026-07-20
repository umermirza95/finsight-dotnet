using Finsight.Interfaces;

namespace Finsight.Services
{
    public class AutoTradeBackgroundService : BackgroundService
    {
        private readonly ILogger<AutoTradeBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public AutoTradeBackgroundService(ILogger<AutoTradeBackgroundService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AutoTradeBackgroundService is starting.");

            using var scope = _serviceProvider.CreateScope();
            var tradingService = scope.ServiceProvider.GetRequiredService<ITradingService>();
            var brokerService = scope.ServiceProvider.GetRequiredService<IBrokerService>();

            try
            {
                var config = await tradingService.GetTradingConfigAsync();
                if (config != null && config.AutoTrade)
                {
                    _logger.LogInformation("AutoTrade is enabled. Connecting to IBKR...");
                    brokerService.Connect();
                }
                else
                {
                    _logger.LogInformation("AutoTrade is disabled or not configured.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Trading Config on startup.");
            }

            // Keep the service alive and handle auto-reconnects
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(10000, stoppingToken);

                try
                {
                    var config = await tradingService.GetTradingConfigAsync();
                    if (config != null && config.AutoTrade && !brokerService.IsConnected)
                    {
                        _logger.LogWarning("IBKR connection dropped while AutoTrade is enabled. Attempting to reconnect...");
                        brokerService.Connect();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during auto-reconnect check.");
                }
            }

            _logger.LogInformation("AutoTradeBackgroundService is stopping.");
            brokerService.Disconnect();
        }
    }
}
