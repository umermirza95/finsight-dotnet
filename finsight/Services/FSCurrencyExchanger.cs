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
                float fxRate = await FetchLiveFXRate(command.Currency.GetValueOrDefault(FSSupportedCurrencies.USD), FSSupportedCurrencies.USD, date);
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

        public async Task<float> FetchLiveFXRate(FSSupportedCurrencies from, FSSupportedCurrencies to, DateTime date)
        {
            if (from == to)
            {
                return 1;
            }
            var client = httpClientFactory.CreateClient();
            string apiKey = await secretsProvider.GetSecretAsync("WISE_API_KEY");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            var response = await client.GetAsync($"{CONSTANTS.WISE_FX_URL}/rates?source=${from}&target=USD&time=${date:O}");
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to acquire live FX rate for {from}");
            }
            var jsonString = await response.Content.ReadAsStringAsync();
            var list = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonString);
            object? rate = (list?.ElementAt(0)?.TryGetValue("rate", out rate)) ?? throw new Exception($"Failed to acquire live FX rate for {from}");
            return (float)rate;
        }
    }
}