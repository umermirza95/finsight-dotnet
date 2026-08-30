using Finsight.Commands;
using Finsight.Models;

namespace Finsight.Interfaces
{
    public interface IWalletService
    {
        Task<FSWallet> CreateWalletAsync(CreateWalletCommand command, string userId);
        Task<IEnumerable<FSWallet>> GetWalletsAsync(string userId);
        Task<FSWallet> GetWalletAsync(Guid walletId, string userId);
    }
}
