
using Finsight.Models;

namespace Finsight.Services
{
    public interface IExchangeRateService
    {
        public Task<FSExchangeRate> GetExchangeRateAsync(FSCurrency from, FSCurrency to, DateOnly date);
        public Task<List<FSExchangeRate>> GetExchangeRatesAsync(FSCurrency from, List<FSCurrency> toList, DateOnly date);
        void LoadAllExchangeRates();

        List<FSCurrency> SupportedCurrencies { get; }
    }
}