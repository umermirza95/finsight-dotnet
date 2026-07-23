using System.Collections.Generic;
using System.Threading.Tasks;
using Finsight.Models;
using Finsight.Queries;
using Finsight.DTOs;

namespace Finsight.Interfaces
{
    public interface ITradingService
    {
        Task FetchMonthlyTradesAsync(string userId);
        Task MatchClosedTradesAsync(string userId);
        Task<List<OpenTradeDTO>> GetOpenTradesAsync(string userId);
        Task<List<ClosedTradeResponse>> GetClosedTradesAsync(string userId, GetTradesQuery query);
        Task<FSTradingConfig?> GetTradingConfigAsync();
        Task<FSTradingConfig> UpdateTradingConfigAsync(UpdateTradingConfigDTO dto);
        Task HandleTradeExecutionAsync(FSTrade trade);
    }
}
