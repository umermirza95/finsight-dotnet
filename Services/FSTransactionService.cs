using Finsight.Commands;
using Finsight.DTOs;
using Finsight.Interfaces;
using Finsight.Models;
using Finsight.Queries;
using Microsoft.EntityFrameworkCore;

namespace Finsight.Services
{
    public class FSTransactionService(AppDbContext context, IExchangeRateService fxService) : ITransactionService
    {
        private readonly AppDbContext _context = context;
        private readonly IExchangeRateService exchangeRateService = fxService;

        public async Task<IEnumerable<FSTransactionDTO>> GetTransactionsInDefaultCurrencyAsync(GetTransactionsQuery query, string userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var fxRates = new Dictionary<string, decimal>();
            var defaultCurrency = user?.DefaultCurrency ?? "USD";
            var transactionDTOs = (await GetTransactionsAsync(query, userId)).Select(t => new FSTransactionDTO
            {
                Amount = t.Amount,
                BaseAmount = t.Amount,
                Comment = t.Comment,
                Date = t.Date,
                CategoryId = t.FSCategoryId,
                Currency = t.FSCurrencyCode,
                SubCategoryId = t.FSSubCategoryId,
                Id = t.Id,
                Mode = t.Mode,
                SubType = t.SubType,
                Type = t.Type
            }).ToList();
            foreach (var transaction in transactionDTOs)
            {
                if (transaction.Currency != defaultCurrency)
                {
                    DateOnly transactionDate = DateOnly.FromDateTime(transaction.Date);
                    string key = FSHelpers.GetFXKey(transaction.Currency, defaultCurrency, transactionDate);
                    decimal rate = fxRates.GetValueOrDefault(key);
                    if (rate <= 0)
                    {
                        rate = (await exchangeRateService.GetExchangeRateAsync(new FSCurrency { Code = transaction.Currency }, new FSCurrency { Code = defaultCurrency }, DateOnly.FromDateTime(transaction.Date))).ExchangeRate;
                        fxRates.Add(key, rate);
                    }
                    transaction.Amount *= rate;
                }
            }
            return transactionDTOs;
        }

        public async Task<IEnumerable<FSTransaction>> GetTransactionsAsync(GetTransactionsQuery query, string userId)
        {
            var startDate = query.From?.ToUniversalTime();
            var endDate = query.To?.ToUniversalTime();
            var q = _context.Transactions
            .Where(t => t.FSUserId == userId)
            .Where(t => t.Date >= startDate && t.Date <= endDate);

            if (query.Type != null)
                q = q.Where(t => t.Type == query.Type);

            if (query.CategoryId != null)
                q = q.Where(t => t.FSCategoryId == query.CategoryId);

            var result = await q
                .OrderByDescending(t => t.Date)
                .ToListAsync();

            return result;
        }

        public async Task<FSTransaction> AddTransactionWithFXAsync(CreateTransactionCommand command, string userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            DateTime transactionDate = command.Date ?? DateTime.UtcNow;
            FSCurrency transactionCurrency = new FSCurrency { Code = command.Currency };
            FSCurrency defaultCurrency = new() { Code = user?.DefaultCurrency ?? "USD" };
            DateOnly exchangeDate = DateOnly.FromDateTime(transactionDate);
            if (command.Currency != defaultCurrency.Code)
            {
                await exchangeRateService.GetExchangeRateAsync(transactionCurrency, defaultCurrency, exchangeDate);
            }
            return await AddTransactionAsync(command, userId);
        }


        public async Task<FSTransaction> AddTransactionAsync(CreateTransactionCommand command, string userId)
        {
            var transaction = new FSTransaction
            {
                Id = Guid.NewGuid(),
                FSUserId = userId,
                Amount = command.Amount,
                FSCategoryId = command.CategoryId,
                FSSubCategoryId = command.SubCategoryId,
                FSCurrencyCode = command.Currency,
                Comment = command.Comment,
                Type = command.Type,
                SubType = command.SubType,
                Mode = command.Mode,
                Date = command.Date ?? DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }
    }
}