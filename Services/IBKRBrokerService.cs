
using System.Xml.Linq;
using Finsight.Enums;
using Finsight.Interfaces;
using Finsight.Models;
using IBApi;
using Microsoft.EntityFrameworkCore;
using Finsight.DTOs;

namespace Finsight.Services
{
    public class IBKRBrokerService : IBrokerService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<IBKRBrokerService> _logger;
        private readonly IBKR.IIBKRConnectionManager _connectionManager;
        private readonly IMessagingService _messagingService;

        public IBKRBrokerService(HttpClient httpClient, AppDbContext dbContext, IConfiguration configuration, ILogger<IBKRBrokerService> logger, IBKR.IIBKRConnectionManager connectionManager, IMessagingService messagingService)
        {
            _httpClient = httpClient;
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
            _connectionManager = connectionManager;
            _messagingService = messagingService;
        }

        public bool IsConnected(string userId) 
        {
            var handler = _connectionManager.GetHandler(userId);
            return handler != null && handler.Client.IsConnected();
        }

        public void Connect(string host, int port, int clientId, string userId)
        {
            var handler = _connectionManager.GetOrCreateHandler(userId);
            handler.Connect(host, port, clientId);
        }

        public void Disconnect(string userId)
        {
            _connectionManager.RemoveHandler(userId);
        }

        public async Task PlaceLimitOrderAsync(string userId, string ticker, TradeDirection direction, decimal limitPrice, decimal quantity, bool logsOnly, string? account = null)
        {
            var handler = _connectionManager.GetHandler(userId);
            if (handler == null || !handler.Client.IsConnected())
                throw new Exception("IBKR is not connected.");

            await _messagingService.SendMessageAsync($"*Action Intent*: Placing ${direction} limit order for {quantity} shares of {ticker} at ${limitPrice}.");

            if (logsOnly)
                return;

            var contract = new Contract
            {
                Symbol = ticker,
                SecType = "STK",
                Exchange = "SMART",
                Currency = "USD"
            };

            var order = new Order
            {
                Action = direction == TradeDirection.BUY ? "BUY" : "SELL",
                OrderType = "LMT",
                TotalQuantity = (double)quantity,
                LmtPrice = (double)limitPrice,
                Tif = "GTC",
                OutsideRth = true
            };

            // Hardcoded default account as requested
            order.Account = !string.IsNullOrEmpty(account) ? account : "U7630023";

            var orderId = handler.GetNextOrderId();
            
            // Set up the wait task before sending the order to avoid race conditions
            var waitTask = handler.WaitForOrderPlacementAsync(orderId, TimeSpan.FromSeconds(10));
            
            handler.Client.placeOrder(orderId, contract, order);

            // Wait for confirmation or error from IBKR
            await waitTask;

            _logger.LogInformation($"Placed limit order {orderId} for {ticker} {direction} {quantity} @ {limitPrice}");
        }

        public async Task CancelAllOrdersAsync(string userId, bool logsOnly)
        {
            await _messagingService.SendMessageAsync("*Action Intent*: Canceling all open orders.");
            if(logsOnly)
                return;

            var handler = _connectionManager.GetHandler(userId);
            if (handler == null || !handler.Client.IsConnected())
                throw new Exception("IBKR is not connected.");


            handler.Client.reqGlobalCancel();
            _logger.LogInformation("Requested global cancel of all open orders.");
        }

        public async Task<List<ActiveOrderDTO>> GetActiveOrdersAsync(string userId)
        {
            var handler = _connectionManager.GetHandler(userId);
            if (handler == null || !handler.Client.IsConnected())
                return new List<ActiveOrderDTO>();

            // Trigger a refresh and wait for IBKR to send all open orders
            var rawOrders = await handler.RefreshAndGetOpenOrdersAsync();

            var openOrders = rawOrders.Select(o => new ActiveOrderDTO
            {
                OrderId = o.OrderId,
                Ticker = o.Contract.Symbol,
                Action = o.Order.Action,
                Quantity = (decimal)o.Order.TotalQuantity,
                LimitPrice = (decimal)o.Order.LmtPrice
            }).ToList();

            return openOrders;
        }

        public async Task AdjustOrderPriceAsync(string userId, int permId, decimal newPrice)
        {
            var handler = _connectionManager.GetHandler(userId);
            if (handler == null || !handler.Client.IsConnected())
                throw new Exception("IBKR is not connected.");

            var openOrders = handler.GetOpenOrders();
            var targetOrder = openOrders.FirstOrDefault(o => o.OrderId == permId); // Note: Tuple's OrderId field contains the PermId because of GetOpenOrders() mapping

            if (targetOrder.Order == null)
            {
                throw new Exception($"Active order with PermId {permId} not found.");
            }

            if (targetOrder.Order.OrderId == 0)
            {
                throw new InvalidOperationException("Cannot adjust price of orders placed manually or externally. Only API-originated orders can be modified without binding.");
            }

            targetOrder.Order.LmtPrice = (double)newPrice;
            
            var waitTask = handler.WaitForOrderPlacementAsync(targetOrder.Order.OrderId, TimeSpan.FromSeconds(10));
            handler.Client.placeOrder(targetOrder.Order.OrderId, targetOrder.Contract, targetOrder.Order);
            
            // Wait for confirmation or error from IBKR
            await waitTask;
        }

        public async Task CancelOrderAsync(string userId, int permId)
        {
            var handler = _connectionManager.GetHandler(userId);
            if (handler == null || !handler.Client.IsConnected())
                throw new Exception("IBKR is not connected.");

            var openOrders = handler.GetOpenOrders();
            var targetOrder = openOrders.FirstOrDefault(o => o.OrderId == permId);

            if (targetOrder.Order == null)
            {
                throw new Exception($"Active order with PermId {permId} not found.");
            }

            if (targetOrder.Order.OrderId == 0)
            {
                throw new InvalidOperationException("Cannot cancel orders placed manually or externally. Only API-originated orders can be cancelled without binding.");
            }

            var waitTask = handler.WaitForOrderPlacementAsync(targetOrder.Order.OrderId, TimeSpan.FromSeconds(10));
            handler.Client.cancelOrder(targetOrder.Order.OrderId);
            
            // Wait for confirmation or error from IBKR
            await waitTask;
            
            _logger.LogInformation($"Requested cancel of order {permId}");
        }

        public async Task FetchMonthlyTradesAsync(string userId)
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning($"User {userId} not found.");
                return;
            }

            var handler = _connectionManager.GetHandler(userId);
            if (handler == null || !handler.Client.IsConnected())
            {
                _logger.LogWarning("Cannot fetch today's trades. IBKR is not connected.");
                throw new Exception("IBKR is not connected.");
            }

            _logger.LogInformation("Fetching today's executions from IBKR via API.");
            var executionsData = await handler.GetExecutionsAsync();
            var newTrades = new List<FSTrade>();

            var grouped = executionsData.GroupBy(e => e.Execution.PermId.ToString()).ToList();
            var orderIds = grouped.Select(g => g.Key).ToList();

            var existingIds = await _dbContext.FSTrades
                .Where(t => orderIds.Contains(t.ExternalId))
                .Select(t => t.ExternalId)
                .ToListAsync();

            foreach (var group in grouped)
            {
                if (!existingIds.Contains(group.Key))
                {
                    var first = group.First();
                    var direction = (first.Execution.Side.Equals("BOT", StringComparison.OrdinalIgnoreCase) || first.Execution.Side.Equals("BUY", StringComparison.OrdinalIgnoreCase)) 
                        ? TradeDirection.BUY : TradeDirection.SELL;

                    var totalQty = group.Sum(x => (decimal)x.Execution.Shares);
                    var totalValue = group.Sum(x => (decimal)x.Execution.Shares * (decimal)x.Execution.Price);
                    var vwap = totalQty > 0 ? totalValue / totalQty : (decimal)first.Execution.Price;
                    var totalComm = 0m; 

                    DateTime parsedDate = DateTime.UtcNow;
                    try
                    {
                        if (!string.IsNullOrEmpty(first.Execution.Time))
                        {
                            string timeStr = first.Execution.Time;
                            if (DateTime.TryParseExact(timeStr, "yyyyMMdd  HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var pd))
                            {
                                parsedDate = pd;
                            }
                            else if (DateTime.TryParseExact(timeStr, "yyyyMMdd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var pd2))
                            {
                                parsedDate = pd2;
                            }
                        }
                    }
                    catch { }

                    newTrades.Add(new FSTrade
                    {
                        Id = Guid.NewGuid(),
                        FSUserId = userId,
                        Ticker = first.Contract.Symbol,
                        TradePrice = vwap,
                        TradeDirection = direction,
                        Quantity = totalQty,
                        Commission = totalComm,
                        Date = parsedDate,
                        ExternalId = group.Key
                    });
                }
            }

            if (newTrades.Any())
            {
                _dbContext.FSTrades.AddRange(newTrades);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"Inserted {newTrades.Count} new trades from IBKR API for today.");
            }
            else
            {
                _logger.LogInformation("No new trades to insert from IBKR API for today.");
            }
        }
    }
}
