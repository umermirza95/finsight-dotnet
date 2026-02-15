
using System.Net.Http.Headers;
using System.Text.Json;
using Finsight.Commands;
using Finsight.Interfaces;
using Finsight.Models;
using Microsoft.EntityFrameworkCore;

namespace Finsight.Services
{

    public class FSExchangeRateService(IDbContextFactory<AppDbContext> dbFactory, IFXAPIService fxApiService) : IExchangeRateService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;
        private readonly IFXAPIService _fxApiService = fxApiService;
        public List<FSCurrency> SupportedCurrencies
        {
            get
            {
                using var _context = _dbFactory.CreateDbContext();
                return _context.FSCurrencies.ToList();
            }
        }

        public async Task AddMissingFXRatesForTransactionAsync(CreateTransactionCommand transactionCommand, FSUser user)
        {
            using var _context = await _dbFactory.CreateDbContextAsync();
            var targetCurrencyCodes = await _context.FSBudgets
                                    .Where(b => b.FSUserId == user.Id &&
                                    b.StartDate <= transactionCommand.Date &&
                                    b.FSCurrencyCode != transactionCommand.Currency &&
                                    b.BudgetCategories.Any(bc => bc.CategoryId == transactionCommand.CategoryId))
                                    .Select(b => b.FSCurrencyCode)
                                    .Distinct()
                                    .ToListAsync();
            if (transactionCommand.Currency != user.DefaultCurrency && !targetCurrencyCodes.Any(c => c == user.DefaultCurrency))
            {
                targetCurrencyCodes.Add(user.DefaultCurrency);
            }
            if (targetCurrencyCodes.Count != 0)
            {
                await SaveExchangeRatesAsync(transactionCommand.Currency, targetCurrencyCodes, transactionCommand.Date);
            }
        }

        public async Task AddMissingFXRatesForBudgetAsync(CreateBudgetCommand budgetCommand, FSUser user)
        {
            using var _context = await _dbFactory.CreateDbContextAsync();
            var transactionsToSync = await _context.Transactions
                                    .Where(t => t.FSUserId == user.Id &&
                                    t.Date >= budgetCommand.StartDate &&
                                    t.FSCurrencyCode != budgetCommand.CurrencyCode &&
                                    budgetCommand.CategoryIds.Contains(t.FSCategoryId))
                                    .ToListAsync();

            var groupedByCurrency = transactionsToSync.GroupBy(t => t.FSCurrencyCode);
            if (!groupedByCurrency.Any())
                return;

            IEnumerable<Task<FSExchangeRate>>? fetchTasks = [];
            foreach (var group in groupedByCurrency)
            {

            }
        }

        private async Task SaveExchangeRatesAsync(string source, List<string> targetCurrencyCodes, DateOnly date)
        {
            using var _context = await _dbFactory.CreateDbContextAsync();
            var existingRateTargets = await _context.FSExchangeRates
                                .Where(er => er.From == source &&
                                             er.Date == date &&
                                             targetCurrencyCodes.Contains(er.To))
                                .Select(er => er.To)
                                .ToListAsync();
            var missingCurrencyCodes = targetCurrencyCodes.Except(existingRateTargets).ToList();
            if (missingCurrencyCodes.Count != 0)
            {
                var fetchTasks = missingCurrencyCodes.Select(target => _fxApiService.FetchExchangeRateFromAPI(source, target, date));
                FSExchangeRate[] apiRates = await Task.WhenAll(fetchTasks);
                _context.FSExchangeRates.AddRange(apiRates);
                await _context.SaveChangesAsync();
            }
        }

    }
}