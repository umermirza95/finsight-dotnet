using Finsight.Commands;
using Finsight.Interfaces;
using Finsight.Models;
using Mscc.GenerativeAI;

namespace Finsight.Services
{
    public class FSGeminiService : ILLMService
    {
        public async Task<FSTransactionSuggestion> CreateTransactionSuggestionAsync(WebhookEmailCommand email)
        {
            
        }
    }
}