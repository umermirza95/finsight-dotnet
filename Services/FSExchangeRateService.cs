
using System.Net.Http.Headers;
using System.Text.Json;
using Finsight.Models;
using Microsoft.EntityFrameworkCore;

namespace Finsight.Services
{

    public class FSExchangeRateService(HttpClient httpClient, IConfiguration configuration, AppDbContext context) : IExchangeRateService
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly IConfiguration _config = configuration;
        private readonly AppDbContext _context = context;

        private Dictionary<string, decimal> cachedExchangeRates = [];

        public List<FSCurrency> SupportedCurrencies => [.. _context.FSCurrencies];

        public async void LoadAllExchangeRates()
        {
            var currencies = await _context.FSExchangeRates.ToListAsync();
            foreach (var currency in currencies)
            {
                cachedExchangeRates.Add(FSHelpers.GetFXKey(currency), currency.ExchangeRate);
            }
        }

        public async Task<FSExchangeRate> GetExchangeRateAsync(FSCurrency from, FSCurrency to, DateOnly date)
        {
            FSExchangeRate? exchangeRate = await _context.FSExchangeRates.FirstOrDefaultAsync(
                    fx => fx.From == from.Code &&
                    fx.To == to.Code &&
                    fx.Date == date
                );
            if (exchangeRate != null)
            {
                return exchangeRate;
            }
            var apiUrl = $"{_config["ExchangeRateApi:BaseUrl"]}?source={from.Code}&target={to.Code}&time={date.ToString("yyyy-MM-dd")}";
            var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config["ExchangeRateApi:ApiKey"]);
            var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Error fetching exchange rate: {body}");
            using var doc = JsonDocument.Parse(body);
            var rateElement = doc.RootElement[0].GetProperty("rate");
            decimal rate = rateElement.GetDecimal();
            exchangeRate = new FSExchangeRate { Date = date, ExchangeRate = rate, From = from.Code, To = to.Code };
            _context.FSExchangeRates.Add(exchangeRate);
            await _context.SaveChangesAsync();
            return exchangeRate;
        }
    }
}