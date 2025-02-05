
using Finsight.Models;
using Finsight.Query;

namespace Finsight.Interface
{
    public interface FSITransactionRepository
    {
        IAsyncEnumerable<FSTransactionModel> FetchAsync(string userId, FSTransactionQuery query);
    }
}