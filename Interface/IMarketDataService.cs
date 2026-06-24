using System.Collections.Generic;
using System.Threading.Tasks;

namespace Finsight.Interfaces
{
    public interface IMarketDataService
    {
        Task<Dictionary<string, decimal>> GetPricesAsync(IEnumerable<string> tickers);
    }
}
