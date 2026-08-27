using System.Collections.Generic;
using System.Threading.Tasks;
using Finsight.Models;
using Finsight.Queries;
using Finsight.DTOs;

namespace Finsight.Interfaces
{
    public interface ITradingService
    {
      
        Task FetchTodayTradesAsync(string userId);
        Task MatchClosedTradesAsync(string userId);
        Task<List<OpenTradeDTO>> GetOpenTradesAsync(string userId);
        Task<List<ClosedTradeResponse>> GetClosedTradesAsync(string userId, GetTradesQuery query);
        Task<FSTradingConfig?> GetTradingConfigAsync(string userId);
        Task<FSTradingConfig> UpdateTradingConfigAsync(string userId, Finsight.Commands.UpdateTradingConfigCommand dto);
        Task HandleTradeExecutionAsync(FSTrade trade);
        Task ManualMatchTradesAsync(string userId, Finsight.Commands.ManualMatchCommand command);
        Task MakeProfitDistributionAsync(string userId, Finsight.Commands.MakeProfitDistributionCommand command);
        Task<decimal> GetAvailableBalanceAsync(string userId);
        Task<List<ProfitDistributionDTO>> GetProfitDistributionsAsync(string userId, GetProfitDistributionsQuery query);
        Task<List<InsurancePayoutDTO>> GetInsurancePayoutsAsync(string userId, GetInsurancePayoutsQuery query);
        Task<decimal> GetInsuranceBalanceAsync(string userId);
        Task<decimal> ReconcileBalanceWithBrokerAsync(string userId);
        Task<decimal> GetTotalCapitalAsync(string userId);
    }
}
