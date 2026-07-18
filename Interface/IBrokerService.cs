using System.Threading.Tasks;

namespace Finsight.Interfaces
{
    public interface IBrokerService
    {
        Task FetchMonthlyTradesAsync(string userId);
    }
}
