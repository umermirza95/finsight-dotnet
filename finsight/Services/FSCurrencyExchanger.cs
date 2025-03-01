using Finsight.Command;
using Finsight.Interface;
using Finsight.Models;
using Google.Cloud.Firestore;
using Newtonsoft.Json;

namespace Finsight.Service
{
    public class FSCurrencyConverter(FirestoreDb firestore, IHttpClientFactory httpClientFactory, FSISecretsProvider secretsProvider)
    {
        readonly FirestoreDb firestore = firestore;
        private readonly IHttpClientFactory httpClientFactory = httpClientFactory;
        private readonly FSISecretsProvider secretsProvider = secretsProvider;

        public async Task<float> ConvertToUSD(FSCreateTransactionCommand command)
        {
            if (!command.Currency.HasValue || command.Currency == FSSupportedCurrencies.USD)
            {
                return command.Amount;
            }
            DateTime date = command.Date ?? DateTime.Now;
            if (command.UseLiveFx == true)
            {
                double fxRate = await FetchLiveFXRate(command.Currency.GetValueOrDefault(FSSupportedCurrencies.USD), FSSupportedCurrencies.USD, date);
                return (float)Math.Round((command.Amount * fxRate), 2);
            }
            var documentId = $"{date.Year}{date.Month}";
            var snapshot = await firestore.Collection(CONSTANTS.FX_COLLECTION).Document(documentId).GetSnapshotAsync();
            if (!snapshot.Exists || !snapshot.ContainsField(command.Currency.ToString()))
            {
                throw new Exception($"Failed to acquire fixed rate for {command.Currency}");
            }
            float rate = snapshot.GetValue<float>(command.Currency.ToString());
            float converted = command.Amount / rate;
            return (float)Math.Round(converted, 2);
        }

        public async Task<double> FetchLiveFXRate(FSSupportedCurrencies from, FSSupportedCurrencies to, DateTime date)
        {
            if (from == to)
            {
                return 1;
            }
            var client = httpClientFactory.CreateClient();
            string apiKey = await secretsProvider.GetSecretAsync("WISE_API_KEY");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            string url = $"{CONSTANTS.WISE_FX_URL}/rates?source={from}&target=USD&time={date:O}";
            var response = await client.GetAsync(url);
            var jsonString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(jsonString);
            }
            var list = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonString);
            var rate = list?[0]["rate"] ?? throw new Exception($"Failed to acquire live FX rate for {from}");
            return (double)rate;
        }
    }
}