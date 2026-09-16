using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Finsight.Commands;
using Finsight.DTOs;
using Finsight.Enums;
using Finsight.Interfaces;
using Finsight.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Alpaca.Markets;

namespace Finsight.Services
{
    public class FSAlpacaTradingService : IBrokerService
    {
        private readonly AppDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FSAlpacaTradingService> _logger;
        private readonly AlpacaConnectionManager _connectionManager;

        public FSAlpacaTradingService(HttpClient httpClient, AppDbContext dbContext, IConfiguration configuration, ILogger<FSAlpacaTradingService> logger, AlpacaConnectionManager connectionManager)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
            _connectionManager = connectionManager;
        }

        private async Task<IAlpacaTradingClient> GetAlpacaClientAsync(string userId)
        {
            var config = await _dbContext.TradingConfigs.FirstOrDefaultAsync(c => c.FSUserId == userId);
            if (config == null || string.IsNullOrEmpty(config.AlpacaApiKey) || string.IsNullOrEmpty(config.AlpacaApiSecret))
            {
                throw new Exception("Alpaca API credentials are not configured for this user.");
            }

            var secretKey = new SecretKey(config.AlpacaApiKey, config.AlpacaApiSecret);
            return Alpaca.Markets.Environments.Live.GetAlpacaTradingClient(secretKey);
        }

        public bool IsConnected(string userId)
        {
            return _connectionManager.IsConnected(userId);
        }

        public async Task ConnectAsync(string host, int port, int clientId, string userId)
        {
            await _connectionManager.ConnectManualAsync(userId);
        }

        public void Disconnect(string userId)
        {
            _connectionManager.DisconnectUser(userId);
        }

        public async Task PlaceLimitOrderAsync(string userId, string ticker, TradeDirection direction, decimal limitPrice, decimal quantity, string? account = null)
        {
            var client = await GetAlpacaClientAsync(userId);
            var orderSide = direction == TradeDirection.BUY ? OrderSide.Buy : OrderSide.Sell;

            var request = new NewOrderRequest(
                ticker,
                OrderQuantity.Fractional(quantity),
                orderSide,
                OrderType.Limit,
                TimeInForce.Gtc
            )
            {
                LimitPrice = limitPrice,
                ExtendedHours = true
            };

            await client.PostOrderAsync(request);
        }

        public async Task<List<ActiveOrderDTO>> GetActiveOrdersAsync(string userId)
        {
            var client = await GetAlpacaClientAsync(userId);
            var request = new ListOrdersRequest { OrderStatusFilter = OrderStatusFilter.Open };
            var orders = await client.ListOrdersAsync(request);
            orders = orders.Where(o => o.Quantity.HasValue && o.LimitPrice.HasValue).ToList();
            return orders.Select(o => new ActiveOrderDTO
            {
                OrderId = o.OrderId.ToString(),
                ConId = 0,
                Ticker = o.Symbol,
                Action = o.OrderSide.ToString().ToUpper(),
                Quantity = o.Quantity!.Value,
                LimitPrice = o.LimitPrice!.Value
            }).ToList();
        }

        public async Task AdjustOrderPriceAsync(string userId, AdjustOrderPriceCommand command)
        {
            var client = await GetAlpacaClientAsync(userId);
            var request = new ChangeOrderRequest(Guid.Parse(command.OrderId))
            {
                LimitPrice = command.NewPrice,
                Quantity = (long)command.Quantity
            };
            await client.PatchOrderAsync(request);
        }

        public async Task CancelOrderAsync(string userId, string permId)
        {
            var client = await GetAlpacaClientAsync(userId);
            await client.CancelOrderAsync(Guid.Parse(permId));
        }

        public async Task CancelAllOrdersAsync(string userId)
        {
            var client = await GetAlpacaClientAsync(userId);
            await client.CancelAllOrdersAsync();
        }

        public async Task<List<FSTrade>> FetchTodayTradesAsync(string userId)
        {
            var client = await GetAlpacaClientAsync(userId);
            var startOfDay = DateTime.UtcNow.Date.AddDays(-2);

            var request = new AccountActivitiesRequest(AccountActivityType.Fill)
                .WithInterval(new Interval<DateTime>(startOfDay, DateTime.UtcNow));

            var activities = await client.ListAccountActivitiesAsync(request);

            var fetchedTrades = new List<FSTrade>();
            
            var groupedFills = activities
                .Where(a => a.ActivityType == AccountActivityType.Fill)
                .Where(a => a.Symbol != null && a.Quantity != null && a.Price != null && a.OrderId != null)
                .GroupBy(a => a.OrderId!.Value);

            foreach (var group in groupedFills)
            {
                var firstActivity = group.First();
                var direction = firstActivity.Side == OrderSide.Buy ? TradeDirection.BUY : TradeDirection.SELL;
                
                var totalQuantity = group.Sum(a => a.Quantity!.Value);
                var vwap = group.Sum(a => a.Quantity!.Value * a.Price!.Value) / totalQuantity;
                
                // Use the maximum DateTime from the group (latest fill)
                var lastFillDate = group.Max(a => a.ActivityDateTimeUtc);

                fetchedTrades.Add(new FSTrade
                {
                    Id = Guid.NewGuid(),
                    FSUserId = userId,
                    Ticker = firstActivity.Symbol!,
                    TradePrice = vwap,
                    TradeDirection = direction,
                    Quantity = totalQuantity,
                    Commission = 0m,
                    Date = lastFillDate,
                    ExternalId = group.Key.ToString(),
                    SharesLeft = totalQuantity
                });
            }

            return fetchedTrades;
        }

        public async Task<decimal> GetUninvestedCashAsync(string userId)
        {
            var client = await GetAlpacaClientAsync(userId);
            var account = await client.GetAccountAsync();
            return account.TradableCash;
        }

        public async Task<List<FSProfitDistribution>> FetchBrokerFeesAsync(string userId)
        {
            var client = await GetAlpacaClientAsync(userId);
            var threeDaysAgo = DateTime.UtcNow.Date.AddDays(-3);
            var request = new AccountActivitiesRequest(AccountActivityType.FeeInUsd)
                .WithInterval(new Interval<DateTime>(threeDaysAgo, DateTime.UtcNow));
                
            var activities = await client.ListAccountActivitiesAsync(request);
            
            var feeDistributions = new List<FSProfitDistribution>();

            foreach (var activity in activities)
            {
                if (string.IsNullOrEmpty(activity.ActivityId)) continue;

                Guid id = Guid.TryParse(activity.ActivityId, out var parsedGuid)
                    ? parsedGuid
                    : new Guid(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(activity.ActivityId)));

                decimal feeAmount = activity.NetAmount.HasValue 
                    ? Math.Abs(activity.NetAmount.Value) 
                    : (activity.Price.HasValue ? Math.Abs(activity.Price.Value) : 0);
                    
                if (feeAmount == 0) continue;

                feeDistributions.Add(new FSProfitDistribution
                {
                    Id = id,
                    FSUserId = userId,
                    Amount = feeAmount,
                    DistributionType = ProfitDistributionType.BrokerFee,
                    Date = activity.ActivityDateTimeUtc
                });
            }

            return feeDistributions;
        }
    }
}
