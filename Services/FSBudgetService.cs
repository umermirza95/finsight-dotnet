
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
            using var context = await _dbFactory.CreateDbContextAsync();
            var transactionsToSync = await context.Transactions
                .Where(t => t.FSUserId == userId && t.Date >= command.StartDate && t.FSCurrencyCode != command.CurrencyCode)
                .ToListAsync();
            
            FSBudget budget = new()
            {
                Id = Guid.NewGuid(),
                FSUserId = userId,
                Name = command.Name,
                FSCurrencyCode = command.CurrencyCode,
                StartDate = command.StartDate,
                Frequency = command.Frequency
            };
            context.FSBudgets.Add(budget);
            await context.SaveChangesAsync();
            return budget;
        }
    }
}