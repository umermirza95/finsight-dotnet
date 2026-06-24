using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Finsight.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Finsight.Services
{
    public class AlpacaMarketDataService : IMarketDataService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public AlpacaMarketDataService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<Dictionary<string, decimal>> GetPricesAsync(IEnumerable<string> tickers)
        {
            var distinctTickers = tickers.Distinct().ToList();
            if (!distinctTickers.Any()) return new Dictionary<string, decimal>();

            var apiKey = _configuration["Alpaca:ApiKey"];
            var apiSecret = _configuration["Alpaca:ApiSecret"];
            var baseUrl = _configuration["Alpaca:BaseUrl"] ?? "https://data.alpaca.markets";

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            {
                return distinctTickers.ToDictionary(t => t, t => 0m);
            }

            var symbols = string.Join(",", distinctTickers);
            var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/v2/stocks/trades/latest?symbols={symbols}");
            request.Headers.Add("APCA-API-KEY-ID", apiKey);
            request.Headers.Add("APCA-API-SECRET-KEY", apiSecret);

            try
            {
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    return distinctTickers.ToDictionary(t => t, t => 0m);
                }

                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var result = new Dictionary<string, decimal>();

                if (doc.RootElement.TryGetProperty("trades", out var tradesElement))
                {
                    foreach (var ticker in distinctTickers)
                    {
                        if (tradesElement.TryGetProperty(ticker, out var tradeElement))
                        {
                            if (tradeElement.TryGetProperty("p", out var priceElement))
                            {
                                result[ticker] = priceElement.GetDecimal();
                            }
                            else
                            {
                                result[ticker] = 0m;
                            }
                        }
                        else
                        {
                            result[ticker] = 0m;
                        }
                    }
                }

                return result;
            }
            catch
            {
                return distinctTickers.ToDictionary(t => t, t => 0m);
            }
        }
    }
}
