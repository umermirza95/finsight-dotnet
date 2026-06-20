using Finsight.Commands;
using Finsight.DTOs;
using Finsight.Models;
using Finsight.Queries;

namespace Finsight.Interfaces
{
    public interface ITransactionService
    {
   

        public Task<IEnumerable<FSTransaction>> GetTransactionsAsync(GetTransactionsQuery query, string userId);
        Task<FSTransaction> AddTransactionWithFXAsync(CreateTransactionCommand command, string userId);

        Task<IEnumerable<FSTransactionDTO>> GetTransactionsInDefaultCurrencyAsync(GetTransactionsQuery query, string userId);
        Task<bool> DeleteTransactionAsync(Guid id, string userId);
        Task<FSTransaction> EditTransactionAsync(Guid id, EditTransactionCommand command, string userId);
    }
}