
using Finsight.Commands;
using Finsight.DTOs;
using Finsight.Interfaces;
using Finsight.Models;
using Microsoft.EntityFrameworkCore;

namespace Finsight.Services
{
    public class FSBudgetService(IDbContextFactory<AppDbContext> dbFactory, IExchangeRateService exchangeRateService) : IBudgetService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;
        private readonly IExchangeRateService _exchangeRateService = exchangeRateService;
        public async Task<FSBudgetDTO> CreateBudgetAsync(CreateBudgetCommand command, string userId)
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
                StartDate = command.StartDate!.Value,
                Frequency = command.Frequency!.Value,
                BudgetCategories = [.. command.CategoryIds!.Select(categoryId => new FSBudgetCategory
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
                                StartDate = command.StartDate!.Value
                            }
                        ]
            };

            _context.FSBudgets.Add(budget);
            await _context.SaveChangesAsync();
            return await GetBudgetByIdAsync(budgetId, userId);
        }

        public async Task<FSBudgetDTO> GetBudgetByIdAsync(Guid budgetId, string userId, DateOnly? referenceDate = null)
        {
            using var _context = await _dbFactory.CreateDbContextAsync();
            var targetDate = referenceDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

            var budgetData = await _context.FSBudgets
                .Where(b => b.Id == budgetId && b.FSUserId == userId)
                .Select(b => new
                {
                    Budget = b,
                    CurrentPeriod = b.Periods
                        .Where(p => p.StartDate <= targetDate)
                        .OrderByDescending(p => p.StartDate)
                        .FirstOrDefault(),
                    CategoryIds = b.BudgetCategories.Select(bc => bc.CategoryId).ToList()
                })
                .Where(x => x.CurrentPeriod != null)
                .FirstOrDefaultAsync() ?? throw new Exception("Budget not found");

            var startDate = budgetData.Budget.GetStartDate(targetDate);
            var endDate = budgetData.Budget.GetEndDate(targetDate);
            var budgetCurrency = budgetData.Budget.FSCurrencyCode;
            var consumedAmount = await _context.Transactions
                .Where(t => t.FSUserId == userId &&
                            budgetData.CategoryIds.Contains(t.FSCategoryId) &&
                            t.Date >= startDate &&
                            t.Date <= endDate)
                .Select(t => t.FSCurrencyCode == budgetCurrency
                    ? t.Amount
                    : t.Amount * _context.FSExchangeRates
                        .Where(er => er.From == t.FSCurrencyCode &&
                                     er.To == budgetCurrency &&
                                     er.Date == t.Date)
                        .Select(er => er.ExchangeRate)
                        .FirstOrDefault())
                .SumAsync();

            return new FSBudgetDTO
            {
                Id = budgetData.Budget.Id,
                Name = budgetData.Budget.Name,
                TotalAmount = budgetData.CurrentPeriod!.Amount,
                ConsumedAmount = consumedAmount,
                Currency = budgetCurrency,
                StartDate = startDate
            };
        }

        public async Task<List<FSBudgetDTO>> GetActiveBudgetsAsync(string userId, DateOnly? referenceDate = null)
        {
            using var _context = await _dbFactory.CreateDbContextAsync();
            var today = referenceDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

            // 1. Fetch Budget and Period data first
            var rawBudgets = await _context.FSBudgets
                .Where(b => b.FSUserId == userId && b.StartDate <= today)
                .Select(b => new
                {
                    Budget = b,
                    CurrentPeriod = b.Periods
                        .Where(p => p.StartDate <= today)
                        .OrderByDescending(p => p.StartDate)
                        .FirstOrDefault(),
                    CategoryIds = b.BudgetCategories.Select(bc => bc.CategoryId).ToList()
                })
                .Where(x => x.CurrentPeriod != null)
                .ToListAsync();

            if (!rawBudgets.Any()) return new List<FSBudgetDTO>();

            // 2. Identify the global data range and unique categories to minimize transaction fetch
            var allCategoryIds = rawBudgets.SelectMany(x => x.CategoryIds).Distinct().ToList();
            var minDate = rawBudgets.Min(x => x.Budget.GetStartDate(referenceDate));
            var maxDate = rawBudgets.Max(x => x.Budget.GetEndDate(referenceDate));

            // 3. Fetch all transactions for these categories in one go
            var transactions = await _context.Transactions
                .Where(t => t.FSUserId == userId &&
                            allCategoryIds.Contains(t.FSCategoryId) &&
                            t.Date >= minDate &&
                            t.Date <= maxDate)
                .ToListAsync();

            // 4. Fetch all relevant exchange rates in one go
            var transactionCurrencies = transactions.Select(t => t.FSCurrencyCode).Distinct();
            var budgetCurrencies = rawBudgets.Select(b => b.Budget.FSCurrencyCode).Distinct();

            var rates = await _context.FSExchangeRates
                .Where(er => transactionCurrencies.Contains(er.From) &&
                            budgetCurrencies.Contains(er.To) &&
                            er.Date >= minDate && er.Date <= maxDate)
                .ToListAsync();

            // 5. Map results in memory
            return rawBudgets.Select(item =>
            {
                var startDate = item.Budget.GetStartDate(referenceDate);
                var endDate = item.Budget.GetEndDate(referenceDate);
                var budgetCurrency = item.Budget.FSCurrencyCode;

                var consumedAmount = transactions
                    .Where(t => item.CategoryIds.Contains(t.FSCategoryId) &&
                                t.Date >= startDate &&
                                t.Date <= endDate)
                    .Sum(t =>
                    {
                        if (t.FSCurrencyCode == budgetCurrency) return t.Amount;

                        var rate = rates.FirstOrDefault(er =>
                            er.From == t.FSCurrencyCode &&
                            er.To == budgetCurrency &&
                            er.Date == t.Date)?.ExchangeRate;

                        return t.Amount * (rate ?? throw new InvalidOperationException("Exchange rate not found for transaction")); // Use 0 or 1 depending on your fallback preference
                    });

                return new FSBudgetDTO
                {
                    Id = item.Budget.Id,
                    Name = item.Budget.Name,
                    TotalAmount = item.CurrentPeriod!.Amount,
                    ConsumedAmount = consumedAmount,
                    Currency = budgetCurrency,
                    StartDate = startDate
                };
            }).ToList();
        }
    }
}