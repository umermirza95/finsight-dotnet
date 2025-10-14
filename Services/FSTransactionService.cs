using Finsight.Commands;
using Finsight.Interfaces;
using Finsight.Models;
using Microsoft.EntityFrameworkCore;

namespace Finsight.Services
{
    public class FSTransactionService(AppDbContext context, IExchangeRateService fxService) : ITransactionService
    {
        private readonly AppDbContext _context = context;
        private readonly IExchangeRateService exchangeRateService = fxService;
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
                exchangeRate ??= await exchangeRateService.GetExchangeRateAsync(transactionCurrency, defaultCurrency, exchangeDate);
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