using Finsight.Commands;
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

        public async Task<IEnumerable<FSWallet>> GetWalletsAsync(string userId)
        {
            using var _context = await _dbFactory.CreateDbContextAsync();
            return await _context.FSWallets
                .Where(w => w.FSUserId == userId)
                .OrderByDescending(w => w.Order)
                .ThenByDescending(w => w.CreationDate)
                .ToListAsync();
        }

        public async Task<FSWallet> GetWalletAsync(Guid walletId, string userId)
        {
            using var _context = await _dbFactory.CreateDbContextAsync();
            var wallet = await _context.FSWallets
                .FirstOrDefaultAsync(w => w.Id == walletId && w.FSUserId == userId);
                
            if (wallet == null)
            {
                throw new KeyNotFoundException("Wallet not found.");
            }
            
            return wallet;
        }
    }
}
