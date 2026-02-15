using Finsight.Commands;
using Finsight.DTOs;
using Finsight.Interfaces;
using Finsight.Models;
using Finsight.Queries;
using Microsoft.EntityFrameworkCore;

namespace Finsight.Services
{
    public class FSTransactionService(IDbContextFactory<AppDbContext> dbFactory, IExchangeRateService fxService, IFileService fileService) : ITransactionService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;
        private readonly IExchangeRateService exchangeRateService = fxService;
        private readonly IFileService _fileService = fileService;

        public async Task<IEnumerable<FSTransactionDTO>> GetTransactionsInDefaultCurrencyAsync(GetTransactionsQuery query, string userId)
        {
            using var _context = await _dbFactory.CreateDbContextAsync();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId) ?? throw new Exception("User not found");
            var result = await (
                from t in _context.Transactions
                from r in _context.FSExchangeRates
                .Where(r => r.From == t.FSCurrencyCode && r.To == user.DefaultCurrency && r.Date == t.Date)
                .DefaultIfEmpty()
                where t.FSUserId == userId
                && t.Date >= query.From
                && t.Date <= query.To
                && (!query.Type.HasValue || t.Type == query.Type)
                && (!query.CategoryId.HasValue || t.FSCategoryId == query.CategoryId)
                && (string.IsNullOrEmpty(query.SearchQuery) || EF.Functions.ILike(t.Comment!, $"%{query.SearchQuery}%"))
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
                    !string.Equals(x.Transaction.FSCurrencyCode, user.DefaultCurrency, StringComparison.OrdinalIgnoreCase))
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
                Amount = x.Transaction.FSCurrencyCode != user.DefaultCurrency && x.Rate != null
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
            using var _context = await _dbFactory.CreateDbContextAsync();
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
            using var _context = await _dbFactory.CreateDbContextAsync();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId) ?? throw new Exception("User not found");
            return await AddTransactionAsync(command, userId);
        }


        public async Task<FSTransaction> AddTransactionAsync(CreateTransactionCommand command, string userId)
        {
            using var _context = await _dbFactory.CreateDbContextAsync();
            var transactionId = Guid.NewGuid();
            var fsFiles = new List<FSFile>();
            foreach (var file in command.Attachments)
            {
                var filePath = await _fileService.UploadFileAsync(file, userId);
                fsFiles.Add(new FSFile
                {
                    Id = Guid.NewGuid(),
                    FileName = file.Name,
                    FilePath = filePath,
                    UploadedAt = DateTime.UtcNow,
                    FSTransactionId = transactionId,
                    FSUserId = userId
                });
            }
            var transaction = new FSTransaction
            {
                Id = transactionId,
                FSUserId = userId,
                Amount = command.Amount,
                FSCategoryId = command.CategoryId ?? throw new ArgumentNullException("CategoryId is required"),
                FSSubCategoryId = command.SubCategoryId,
                FSCurrencyCode = command.Currency,
                Comment = command.Comment,
                Type = command.Type,
                SubType = command.SubType,
                Mode = command.Mode,
                Date = command.Date,
                UpdatedAt = DateTime.UtcNow,
                Files = fsFiles
            };
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<bool> DeleteTransactionAsync(Guid id, string userId)
        {
            using var _context = await _dbFactory.CreateDbContextAsync();
            var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.Id == id && t.FSUserId == userId);

            if (transaction == null)
                return false;

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}