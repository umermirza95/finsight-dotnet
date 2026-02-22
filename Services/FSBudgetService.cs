
using Finsight.Commands;
using Finsight.Interfaces;
using Finsight.Models;
using Microsoft.EntityFrameworkCore;

namespace Finsight.Services
{
    public class FSBudgetService(IDbContextFactory<AppDbContext> dbFactory, IExchangeRateService exchangeRateService) : IBudgetService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;
        private readonly IExchangeRateService _exchangeRateService = exchangeRateService;
        public async Task<FSBudget> CreateBudgetAsync(CreateBudgetCommand command, string userId)
        {
            using var _context = await _dbFactory.CreateDbContextAsync();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId) ?? throw new Exception("User not found");
            await _exchangeRateService.AddMissingFXRatesForBudgetAsync(command, user);
            var budgetId = Guid.NewGuid();
            FSBudget budget = new()
            {
                Id = budgetId,
                FSUserId = userId,
                Name = command.Name,
                FSCurrencyCode = command.CurrencyCode,
                StartDate = command.StartDate,
                Frequency = command.Frequency,
                BudgetCategories = [.. command.CategoryIds.Select(categoryId => new FSBudgetCategory
                {
                    BudgetId = budgetId,
                    CategoryId = categoryId
                })],
                Periods =
                        [
                            new FSBudgetPeriod
                            {
                                Id = Guid.NewGuid(),
                                BudgetId = budgetId,
                                Amount = command.Amount,
                                StartDate = command.StartDate
                            }
                        ]
            };

            _context.FSBudgets.Add(budget);
            await _context.SaveChangesAsync();
            return budget;
        }
    }
}