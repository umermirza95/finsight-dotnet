
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
            exchangeRate = await FetchExchangeRateFromAPI(from, to, date);
            _context.FSExchangeRates.Add(exchangeRate);
            await _context.SaveChangesAsync();
            return exchangeRate;
        }

        public async Task<List<FSExchangeRate>> GetExchangeRatesAsync(FSCurrency source, List<FSCurrency> targets, DateOnly date)
        {
            var targetCodes = targets.Select(t => t.Code).ToList();
            var existingRates = await _context.FSExchangeRates
                                .Where(fx => fx.From == source.Code &&
                                targetCodes.Contains(fx.To) &&
                                fx.Date == date)
                                .ToListAsync();
            var existingTargetCodes = existingRates.Select(r => r.To).ToHashSet();
            var missingTargets = targets.Where(t => !existingTargetCodes.Contains(t.Code)).ToList();
            if (missingTargets.Count != 0)
            {
                var fetchTasks = missingTargets.Select(target => FetchExchangeRateFromAPI(source, target, date));
                FSExchangeRate[] newRates = await Task.WhenAll(fetchTasks);
                _context.FSExchangeRates.AddRange(newRates);
                await _context.SaveChangesAsync();
                existingRates.AddRange(newRates);
            }
            return existingRates;
        }

        private async Task<FSExchangeRate> FetchExchangeRateFromAPI(FSCurrency from, FSCurrency to, DateOnly date)
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
            decimal rate = rateElement.GetDecimal();
            FSExchangeRate exchangeRate = new FSExchangeRate { Date = date, ExchangeRate = rate, From = from.Code, To = to.Code };
            return exchangeRate;
        }
    }
}