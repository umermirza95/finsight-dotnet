using System.Collections.Generic;
using System.Threading.Tasks;

using Finsight.Enums;
using Finsight.DTOs;

namespace Finsight.Interfaces
{
    public interface IBrokerService
    {
        Task FetchMonthlyTradesAsync(string userId);
        bool IsConnected { get; }
        void Connect();
        void Disconnect();
        Task PlaceLimitOrderAsync(string ticker, TradeDirection direction, decimal limitPrice, decimal quantity, bool logsOnly, string? account = null);
        Task<List<ActiveOrderDTO>> GetActiveOrdersAsync();
        Task AdjustOrderPriceAsync(int permId, decimal newPrice);
        Task CancelOrderAsync(int permId);
        Task CancelAllOrdersAsync(bool logsOnly);
    }
}
