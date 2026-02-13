
using Finsight.Models;

namespace Finsight.Services
{
    public interface IExchangeRateService
    {
        Task SaveExchangeRatesAsync(string from, List<string> toList, DateOnly date);
        Task SaveExchangeRatesForRangeAsync(string source, string target, DateOnly startDate, DateOnly endDate);
        List<FSCurrency> SupportedCurrencies { get; }
    }
}