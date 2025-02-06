
using Finsight.Command;
using Finsight.Models;
using Finsight.Query;

namespace Finsight.Interface
{
    public interface FSITransactionRepository
    {
        IAsyncEnumerable<FSTransactionModel> FetchAsync(string userId, FSTransactionQuery query);
        Task AddAsync(string userId, FSTransactionModel transaction);
    }
}