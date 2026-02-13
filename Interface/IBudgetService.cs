
using Finsight.Commands;
using Finsight.Models;

namespace Finsight.Interfaces
{
    public interface IBudgetService
    {
        Task<FSBudget> CreateBudgetAsync(CreateBudgetCommand command, string userId);
    }
}