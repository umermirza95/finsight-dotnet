using Finsight.Commands;
using Finsight.Models;
using Finsight.Queries;

namespace Finsight.Interfaces
{
    public interface ITransactionService
    {
        public Task<FSTransaction> AddTransactionAsync(CreateTransactionCommand command, string userId);

        public Task<IEnumerable<FSTransaction>> GetTransactionsAsync(GetTransactionsQuery query, string userId);
    }    
}