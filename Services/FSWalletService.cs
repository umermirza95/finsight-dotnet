using Finsight.Commands;
using Finsight.DTOs;
using Finsight.Interfaces;
using Finsight.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Finsight.Services
{
    public class FSWalletService : IWalletService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public FSWalletService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<FSWallet> CreateWalletAsync(CreateWalletCommand command, string userId)
        {
            using var _context = await _dbFactory.CreateDbContextAsync();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId) ?? throw new Exception("User not found");
            
            // Validate currency exists
            var currencyExists = await _context.FSCurrencies.AnyAsync(c => c.Code == command.Currency);
            if (!currencyExists)
            {
                throw new ArgumentException($"Currency {command.Currency} is not supported.");
            }

            var wallet = new FSWallet
            {
                Id = Guid.NewGuid(),
                FSUserId = userId,
                Name = command.Name,
                FSCurrencyCode = command.Currency,
                CreationDate = DateTime.UtcNow,
                InitialBalance = command.InitialBalance
            };

            _context.FSWallets.Add(wallet);
            await _context.SaveChangesAsync();

            return wallet;
        }

        public async Task<IEnumerable<FSWalletDTO>> GetWalletsAsync(string userId)
        {
            using var _context = await _dbFactory.CreateDbContextAsync();
            return await _context.FSWallets
                .Where(w => w.FSUserId == userId)
                .Select(w => new FSWalletDTO
                {
                    Id = w.Id,
                    Name = w.Name,
                    FSCurrencyCode = w.FSCurrencyCode,
                    CreationDate = w.CreationDate,
                    InitialBalance = w.InitialBalance,
                    Order = w.Order,
                    Balance = w.InitialBalance + _context.Transactions
                        .Where(t => t.FSWalletId == w.Id)
                        .Select(t => new
                        {
                            t.Amount,
                            t.Type,
                            Rate = t.FSCurrencyCode == w.FSCurrencyCode ? 1m :
                                _context.FSExchangeRates
                                .Where(r => r.From == t.FSCurrencyCode && r.To == w.FSCurrencyCode && r.Date == t.Date)
                                .Select(r => (decimal?)r.ExchangeRate)
                                .FirstOrDefault() ?? 1m
                        })
                        .Sum(x => (x.Type == Finsight.Enums.FSTransactionType.income || x.Type == Finsight.Enums.FSTransactionType.transfer_in ? 1 : -1) * x.Amount * x.Rate)
                })
                .OrderByDescending(w => w.Order)
                .ThenByDescending(w => w.CreationDate)
                .ToListAsync();
        }

        public async Task<FSWalletDTO?> GetWalletAsync(Guid walletId, string userId)
        {
            using var _context = await _dbFactory.CreateDbContextAsync();
            var wallet = await _context.FSWallets
                .Where(w => w.Id == walletId && w.FSUserId == userId)
                .Select(w => new FSWalletDTO
                {
                    Id = w.Id,
                    Name = w.Name,
                    FSCurrencyCode = w.FSCurrencyCode,
                    CreationDate = w.CreationDate,
                    InitialBalance = w.InitialBalance,
                    Order = w.Order,
                    Balance = w.InitialBalance + _context.Transactions
                        .Where(t => t.FSWalletId == w.Id)
                        .Select(t => new
                        {
                            t.Amount,
                            t.Type,
                            Rate = t.FSCurrencyCode == w.FSCurrencyCode ? 1m :
                                _context.FSExchangeRates
                                .Where(r => r.From == t.FSCurrencyCode && r.To == w.FSCurrencyCode && r.Date == t.Date)
                                .Select(r => (decimal?)r.ExchangeRate)
                                .FirstOrDefault() ?? 1m
                        })
                        .Sum(x => (x.Type == Finsight.Enums.FSTransactionType.income || x.Type == Finsight.Enums.FSTransactionType.transfer_in ? 1 : -1) * x.Amount * x.Rate)
                })
                .FirstOrDefaultAsync();

            return wallet;
        }
    }
}
