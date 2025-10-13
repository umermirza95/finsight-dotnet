using Finsight.Commands;
using Finsight.Models;

namespace Finsight.Interfaces
{
    public interface ITransactionService
    {
        public Task<FSTransaction> AddTransaction(CreateTransactionCommand command);
    }    
}