
using System.Net.Http.Headers;
using System.Text.Json;
using Finsight.Models;

namespace Finsight.Services
{

    public class FSExchangeRateService(HttpClient httpClient, IConfiguration configuration) : IExchangeRateService
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly IConfiguration _config = configuration;
        public async Task<FSExchangeRate> GetExchangeRateAsync(FSCurrency from, FSCurrency to, DateOnly date)
        {
            var apiUrl = $"{_config["ExchangeRateApi:BaseUrl"]}?source={from.Code}&target={to.Code}&time={date.ToString("yyyy-MM-dd")}";
            var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config["ExchangeRateApi:ApiKey"]);
            var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Error fetching exchange rate: {body}");
            using var doc = JsonDocument.Parse(body);
            var rateElement = doc.RootElement[0].GetProperty("rate");
            double exchangeRate = rateElement.GetDouble();
            if (double.IsNaN(exchangeRate))
                throw new Exception("Exchange rate fetch failed.");
            return new FSExchangeRate { Date = date, ExchangeRate = exchangeRate, From = from.Code, To = to.Code };
        }
    }
}