using Finsight.Commands;
using Finsight.Interfaces;
using Finsight.Models;

namespace Finsight.Services
{
    public class FSTransactionService : ITransactionService
    {
        public async Task<FSTransaction> AddTransaction(CreateTransactionCommand command)
        {
            return new FSTransaction { FSCurrencyCode = "" };
        }
    }
}