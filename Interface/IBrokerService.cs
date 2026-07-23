using System.Threading.Tasks;

using Finsight.Enums;

namespace Finsight.Interfaces
{
    public interface IBrokerService
    {
        Task FetchMonthlyTradesAsync(string userId);
        bool IsConnected { get; }
        void Connect();
        void Disconnect();
        Task PlaceLimitOrderAsync(string ticker, TradeDirection direction, decimal limitPrice, decimal quantity, bool logsOnly);
        Task CancelAllOrdersAsync();
    }
}
