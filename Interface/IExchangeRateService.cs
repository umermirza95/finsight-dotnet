
using Finsight.Models;

namespace Finsight.Services
{
    public interface IExchangeRateService
    {
        public Task<FSExchangeRate> GetExchangeRateAsync(FSCurrency from, FSCurrency to, DateOnly date);
        void LoadAllExchangeRates();
    }
}