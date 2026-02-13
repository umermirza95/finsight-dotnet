
using Finsight.Models;

namespace Finsight.Interfaces
{
    public interface IFXAPIService
    {
        Task<FSExchangeRate> FetchExchangeRateFromAPI(string source, string target, DateOnly date);
        Task<List<FSExchangeRate>> FetchExchangeRateRangeFromAPI(string source, string target, DateOnly startDate, DateOnly endDate);
    }
}