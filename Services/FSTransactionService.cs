using Finsight.Commands;
using Finsight.Interfaces;
using Finsight.Models;

namespace Finsight.Services
{
    public class FSTransactionService(AppDbContext context) : ITransactionService
    {
        private readonly AppDbContext _context = context;
        public async Task<FSTransaction> AddTransactionAsync(CreateTransactionCommand command, string userId)
        {
            var transaction = new FSTransaction
            {
                Id = Guid.NewGuid(),
                FSUserId = userId,
                Amount = command.Amount,
                FSCategoryId = command.CategoryId ,
                FSSubCategoryId = command.SubCategoryId,
                FSCurrencyCode = command.Currency,
                Comment = command.Comment,
                Type = command.Type,
                SubType = command.SubType,
                Mode = command.Mode,
                Date = command.Date ?? DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Save
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return transaction;
        }
    }
}