
using Finsight.Commands;
using Finsight.DTOs;
using Finsight.Models;

namespace Finsight.Interfaces
{
    public interface IBudgetService
    {
        Task<FSBudgetDTO> CreateBudgetAsync(CreateBudgetCommand command, string userId);
        Task<FSBudgetDTO> GetBudgetByIdAsync(Guid budgetId, string userId, DateOnly? referenceDate = null);
        Task<List<FSBudgetDTO>> GetActiveBudgetsAsync(string userId, DateOnly? referenceDate = null);
    }
}