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
            string defaultCurrency = user?.DefaultCurrency ?? "USD";
            var result = await (
      from t in _context.Transactions
      from r in _context.FSExchangeRates
          .Where( r => r.From == t.FSCurrencyCode && r.To == defaultCurrency && r.Date == t.Date)
          .DefaultIfEmpty()
      where t.Date >= query.From
         && t.Date <= query.To
         && (!query.Type.HasValue || t.Type == query.Type)
         && (!query.CategoryId.HasValue || t.FSCategoryId == query.CategoryId)
      select new
      {
          Transaction = t,
          Rate = r
      }
  )
  .AsNoTracking()
  .ToListAsync();

            // Check and throw for missing exchange rates
            var missingRates = result
                .Where(x =>
                    x.Rate == null &&
                    !string.Equals(x.Transaction.FSCurrencyCode, defaultCurrency, StringComparison.OrdinalIgnoreCase))
                .Select(x => new
                {
                    x.Transaction.Id,
                    x.Transaction.FSCurrencyCode,
                    x.Transaction.Date
                })
                .ToList();

            if (missingRates.Count != 0)
            {
                var details = string.Join(", ", missingRates.Select(m => $"{m.FSCurrencyCode} ({m.Date:yyyy-MM-dd})"));
                throw new Exception($"Missing exchange rate(s) for: {details}");
            }

            // Build the DTO list
            var dtoList = result.Select(x => new FSTransactionDTO
            {
                Id = x.Transaction.Id,
                BaseAmount = x.Transaction.Amount,
                Amount = x.Transaction.FSCurrencyCode != defaultCurrency && x.Rate != null
                    ? x.Transaction.Amount * x.Rate.ExchangeRate
                    : x.Transaction.Amount,
                CategoryId = x.Transaction.FSCategoryId,
                Mode = x.Transaction.Mode,
                Date = x.Transaction.Date,
                Currency = x.Transaction.FSCurrencyCode,
                Type = x.Transaction.Type,
                SubCategoryId = x.Transaction.FSSubCategoryId,
                Comment = x.Transaction.Comment,
                SubType = x.Transaction.SubType
            })
            .OrderByDescending(t => t.Date)
            .ToList();

            return dtoList;
        }

        public async Task<IEnumerable<FSTransaction>> GetTransactionsAsync(GetTransactionsQuery query, string userId)
        {
            var startDate = query.From;
            var endDate = query.To;
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
            DateOnly transactionDate = command.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);
            FSCurrency transactionCurrency = new() { Code = command.Currency };
            FSCurrency defaultCurrency = new() { Code = user?.DefaultCurrency ?? "USD" };
            DateOnly exchangeDate = transactionDate;
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
                Date = command.Date ?? DateOnly.FromDateTime(DateTime.UtcNow),
                UpdatedAt = DateTime.UtcNow
            };
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }
    }
}