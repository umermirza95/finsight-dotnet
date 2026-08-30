using Finsight.Commands;
using Finsight.Models;

namespace Finsight.Interfaces
{
    public interface ILLMService
    {
        Task<FSTransactionSuggestion?> CreateTransactionSuggestionAsync(FSTransactionEmail email);
        Task<List<FSImportedTransaction>> ParseBankStatementAsync(Stream pdfStream, string userId, Guid walletId);
    }
}