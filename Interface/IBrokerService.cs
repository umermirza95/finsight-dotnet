using System.Collections.Generic;
using System.Threading.Tasks;

using Finsight.Enums;
using Finsight.DTOs;
using Finsight.Models;
using Finsight.Commands;

namespace Finsight.Interfaces
{
    public interface IBrokerService
    {
        
        Task<List<FSTrade>> FetchTodayTradesAsync(string userId);
        bool IsConnected(string userId);
        Task ConnectAsync(string host, int port, int clientId, string userId);
        void Disconnect(string userId);
        Task PlaceLimitOrderAsync(string userId, string ticker, TradeDirection direction, decimal limitPrice, decimal quantity, string? account = null);
        Task<List<ActiveOrderDTO>> GetActiveOrdersAsync(string userId);
        Task AdjustOrderPriceAsync(string userId, AdjustOrderPriceCommand command);
        Task CancelOrderAsync(string userId, string permId);
        Task CancelAllOrdersAsync(string userId);
        Task<decimal> GetUninvestedCashAsync(string userId);
    }
}
