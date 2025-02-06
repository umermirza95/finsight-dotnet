using Finsight.Command;
using Finsight.Models;

namespace Finsight.Service
{
    public class FSCurrencyConverter
    {
        public async Task<float> ConvertToUSD(FSCreateTransactionCommand command)
        {
            return command.Amount;
        }
    }
}