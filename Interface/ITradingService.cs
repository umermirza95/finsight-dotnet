using System.Threading.Tasks;

namespace Finsight.Interfaces
{
    public interface ITradingService
    {
        Task FetchMonthlyTradesAsync(string userId);
    }
}
