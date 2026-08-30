using Finsight.Commands;
using Finsight.Models;

namespace Finsight.Interfaces
{
    public interface IWalletService
    {
        Task<FSWallet> CreateWalletAsync(CreateWalletCommand command, string userId);
        Task<IEnumerable<Finsight.DTOs.FSWalletDTO>> GetWalletsAsync(string userId);
        Task<Finsight.DTOs.FSWalletDTO?> GetWalletAsync(Guid walletId, string userId);
    }
}
