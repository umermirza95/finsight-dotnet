using System.Collections.Generic;
using System.Threading.Tasks;

using Finsight.Enums;
using Finsight.DTOs;
using Finsight.Models;

namespace Finsight.Interfaces
{
    public interface IBrokerService
    {
        
        Task<List<FSTrade>> FetchTodayTradesAsync(string userId);
        bool IsConnected(string userId);
        void Connect(string host, int port, int clientId, string userId);
        void Disconnect(string userId);
        Task PlaceLimitOrderAsync(string userId, string ticker, TradeDirection direction, decimal limitPrice, decimal quantity, bool logsOnly, string? account = null);
        Task<List<ActiveOrderDTO>> GetActiveOrdersAsync(string userId);
        Task AdjustOrderPriceAsync(string userId, int permId, decimal newPrice);
        Task CancelOrderAsync(string userId, int permId);
        Task CancelAllOrdersAsync(string userId, bool logsOnly);
    }
}
