
namespace Finsight.Services
{
    public class FSStartupLoader(IExchangeRateService exchangeRateService) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            exchangeRateService.LoadAllExchangeRates();
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}