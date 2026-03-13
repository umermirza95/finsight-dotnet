using Finsight.Commands;
using Finsight.Models;

namespace Finsight.Interfaces
{
    public interface ILLMService
    {
        Task<FSTransactionSuggestion?> CreateTransactionSuggestionAsync(string email, string userId);
    }
}