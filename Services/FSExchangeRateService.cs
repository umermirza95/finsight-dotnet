
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

        public async Task<List<FSExchangeRate>> GetExchangeRateForRangeAsync(FSCurrency from, FSCurrency to, DateOnly startDate, DateOnly endDate)
        {
            var existingRates = await _context.FSExchangeRates
                                .Where(fx => fx.From == from.Code &&
                                fx.To == to.Code &&
                                fx.Date >= startDate &&
                                fx.Date <= endDate)
                                .ToListAsync();
            var existingDates = existingRates.Select(r => r.Date).ToHashSet();
            List<DateOnly> missingDates = [];
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if (!existingDates.Contains(date))
                    missingDates.Add(date);
            }
            if (missingDates.Count != 0)
            {
                var fetchTasks = missingDates.Select(date => FetchExchangeRateFromAPI(from, to, date));
                FSExchangeRate[] newRates = await Task.WhenAll(fetchTasks);
                _context.FSExchangeRates.AddRange(newRates);
                await _context.SaveChangesAsync();
                existingRates.AddRange(newRates);
            }
            return existingRates;
        }

        public async Task<List<FSExchangeRate>> GetExchangeRatesForRangeAsync(List<FSCurrency> from, FSCurrency to, DateOnly startDate, DateOnly endDate)
        {
            var fromCodes = from.Select(f => f.Code).ToList();
            var existingRates = await _context.FSExchangeRates
                                .Where(fx => fromCodes.Contains(fx.From) &&
                                fx.To == to.Code &&
                                fx.Date >= startDate &&
                                fx.Date <= endDate)
                                .ToListAsync();
            var existingFromDatePairs = existingRates.Select(r => (r.From, r.Date)).ToHashSet();
            List<(FSCurrency From, DateOnly Date)> missingFromDatePairs = [];
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                foreach (var source in from)
                {
                    if (!existingFromDatePairs.Contains((source.Code, date)))
                        missingFromDatePairs.Add((source, date));
                }
            }
            if (missingFromDatePairs.Count != 0)
            {
                var fetchTasks = missingFromDatePairs.Select(pair => FetchExchangeRateFromAPI(pair.From, to, pair.Date));
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
            FSExchangeRate exchangeRate = new() { Date = date, ExchangeRate = rate, From = from.Code, To = to.Code };
            return exchangeRate;
        }

        private async Task<List<FSExchangeRate>> FetchExchangeRateRangeFromAPI(FSCurrency from, FSCurrency to, DateOnly startDate, DateOnly endDate)
        {
            var apiUrl = $"{_config["ExchangeRateApi:BaseUrl"]}?source={from.Code}&target={to.Code}&from={startDate.ToString("yyyy-MM-dd")}&to={endDate.ToString("yyyy-MM-dd")}&group=day";
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
                    From = from.Code,
                    To = to.Code
                });
            }
            return exchangeRates;
        }
    }
}