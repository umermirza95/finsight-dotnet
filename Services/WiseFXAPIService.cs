
using System.Net.Http.Headers;
using System.Text.Json;
using Finsight.Interfaces;
using Finsight.Models;

namespace Finsight.Services
{
    public class WiseFXAPIService(HttpClient httpClient, IConfiguration configuration) : IFXAPIService
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly IConfiguration _config = configuration;


        public async Task<FSExchangeRate> FetchExchangeRateFromAPI(string source, string target, DateOnly date)
        {
            var apiUrl = $"{_config["ExchangeRateApi:BaseUrl"]}?source={source}&target={target}&time={date.ToString("yyyy-MM-dd")}";
            var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config["ExchangeRateApi:ApiKey"]);
            var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Error fetching exchange rate: {body}");
            using var doc = JsonDocument.Parse(body);
            var rateElement = doc.RootElement[0].GetProperty("rate");
            decimal rate = rateElement.GetDecimal();
            FSExchangeRate exchangeRate = new() { Date = date, ExchangeRate = rate, From = source, To = target };
            return exchangeRate;
        }

        public async Task<List<FSExchangeRate>> FetchExchangeRateRangeFromAPI(string source, string target, DateOnly startDate, DateOnly endDate)
        {
            var apiUrl = $"{_config["ExchangeRateApi:BaseUrl"]}?source={source}&target={target}&from={startDate.ToString("yyyy-MM-dd")}&to={endDate.ToString("yyyy-MM-dd")}&group=day";
            var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config["ExchangeRateApi:ApiKey"]);
            var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Error fetching exchange rate: {body}");
            using var doc = JsonDocument.Parse(body);
            var exchangeRates = new List<FSExchangeRate>();

            // Wise returns an array of rate objects
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                decimal rate = element.GetProperty("rate").GetDecimal();
                DateTime time = element.GetProperty("time").GetDateTime();
                exchangeRates.Add(new FSExchangeRate
                {
                    Date = DateOnly.FromDateTime(time),
                    ExchangeRate = rate,
                    From = source,
                    To = target
                });
            }
            return exchangeRates;
        }

    }
}