
using System.Net.Http.Headers;
using System.Text.Json;
using Finsight.Interfaces;
using Finsight.Models;
using Microsoft.EntityFrameworkCore;

namespace Finsight.Services
{

    public class FSExchangeRateService(
        HttpClient httpClient,
        IConfiguration configuration,
        AppDbContext context,
        IFXAPIService fxApiService
        ) : IExchangeRateService
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly IConfiguration _config = configuration;
        private readonly AppDbContext _context = context;
        private readonly IFXAPIService _fxApiService = fxApiService;
        public List<FSCurrency> SupportedCurrencies => [.. _context.FSCurrencies];

        public async Task SaveExchangeRatesAsync(string source, List<string> targetCurrencyCodes, DateOnly date)
        {
            var existingRateTargets = await _context.FSExchangeRates
                                .Where(er => er.From == source &&
                                             er.Date == date &&
                                             targetCurrencyCodes.Contains(er.To))
                                .Select(er => er.To)
                                .ToListAsync();
            var missingCurrencyCodes = targetCurrencyCodes.Except(existingRateTargets).ToList();
            if (missingCurrencyCodes.Count != 0)
            {
                var fetchTasks = missingCurrencyCodes.Select(target => _fxApiService.FetchExchangeRateFromAPI(source, target, date));
                FSExchangeRate[] apiRates = await Task.WhenAll(fetchTasks);
                _context.FSExchangeRates.AddRange(apiRates);
                await _context.SaveChangesAsync();
            }
        }

        public async Task SaveExchangeRatesForRangeAsync(string source, string target, DateOnly startDate, DateOnly endDate)
        {
            var existingRates = await _context.FSExchangeRates
                                .Where(er => er.From == source &&
                                             er.To == target &&
                                             er.Date >= startDate &&
                                             er.Date <= endDate)
                                .ToListAsync();
            
        }

    }
}