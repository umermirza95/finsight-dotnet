
using Finsight.Commands;
using Finsight.Models;

namespace Finsight.Services
{
    public interface IExchangeRateService
    {
        Task AddMissingFXRatesForTransactionAsync(CreateTransactionCommand transactionCommand, FSUser user);
        Task AddMissingFXRatesForBudgetAsync(CreateBudgetCommand budgetCommand, FSUser user);
        List<FSCurrency> SupportedCurrencies { get; }
    }
}