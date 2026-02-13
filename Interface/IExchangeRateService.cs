
using Finsight.Models;

namespace Finsight.Services
{
    public interface IExchangeRateService
    {
        public Task<FSExchangeRate> GetExchangeRateAsync(FSCurrency from, FSCurrency to, DateOnly date);
        public Task<List<FSExchangeRate>> GetExchangeRatesAsync(FSCurrency from, List<FSCurrency> toList, DateOnly date);
        Task<List<FSExchangeRate>> GetExchangeRateForRangeAsync(FSCurrency from, FSCurrency to, DateOnly startDate, DateOnly endDate);
        Task<List<FSExchangeRate>> GetExchangeRatesForRangeAsync(List<FSCurrency> from, FSCurrency to, DateOnly startDate, DateOnly endDate);
        void LoadAllExchangeRates();

        List<FSCurrency> SupportedCurrencies { get; }
    }
}