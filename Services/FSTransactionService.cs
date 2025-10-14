using Finsight.Commands;
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

        public async Task<IEnumerable<FSTransaction>> GetTransactionsAsync(GetTransactionsQuery query, string userId)
        {
            var q = _context.Transactions
            .Where(t => t.FSUserId == userId)
            .Where(t => t.Date >= query.StartDate && t.Date <= query.EndDate);

            if (query.Type != null)
                q = q.Where(t => t.Type == query.Type);

            if (query.CategoryId != null)
                q = q.Where(t => t.FSCategoryId == query.CategoryId);

            var result = await q
                .OrderByDescending(t => t.Date)
                .ToListAsync();

            return result;
        }


        public async Task<FSTransaction> AddTransactionAsync(CreateTransactionCommand command, string userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            DateTime transactionDate = command.Date ?? DateTime.UtcNow;
            FSCurrency transactionCurrency = new FSCurrency { Code = command.Currency };
            FSCurrency defaultCurrency = new() { Code = user?.DefaultCurrency ?? "USD" };
            DateOnly exchangeDate = DateOnly.FromDateTime(transactionDate);
            if (command.Currency != defaultCurrency.Code)
            {
                FSExchangeRate? exchangeRate = await _context.FSExchangeRates.FirstOrDefaultAsync(
                    fx => fx.From == command.Currency &&
                    fx.To == defaultCurrency.Code &&
                    fx.Date == exchangeDate
                );
                if (exchangeRate == null)
                {
                    exchangeRate = await exchangeRateService.GetExchangeRateAsync(transactionCurrency, defaultCurrency, exchangeDate);
                    _context.FSExchangeRates.Add(exchangeRate);
                }

            }
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