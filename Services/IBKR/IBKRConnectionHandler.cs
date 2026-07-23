using System;
using System.Threading;
using IBApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Finsight.Interfaces;

namespace Finsight.Services.IBKR
{
    public class IBKRConnectionHandler : DefaultEWrapper
    {
        private readonly ILogger<IBKRConnectionHandler> _logger;
        private EClientSocket _clientSocket;
        private EReaderSignal _signal;
        private int _nextOrderId;
        
        public EClientSocket Client => _clientSocket;
        
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<int, (Contract Contract, Order Order)> _openOrders = new();
        private readonly IMessagingService _messagingService;

        public IBKRConnectionHandler(ILogger<IBKRConnectionHandler> logger, IServiceScopeFactory scopeFactory, IMessagingService messagingService)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _messagingService = messagingService;
            _signal = new EReaderMonitorSignal();
            _clientSocket = new EClientSocket(this, _signal);
        }

        public void Connect(string host, int port, int clientId)
        {
            if (_clientSocket.IsConnected())
            {
                _logger.LogInformation("Already connected to IBKR.");
                return;
            }

            _logger.LogInformation($"Connecting to IBKR at {host}:{port} with ClientId {clientId}");
            _clientSocket.eConnect(host, port, clientId);
            
            var reader = new EReader(_clientSocket, _signal);
            reader.Start();

            new Thread(() =>
            {
                while (_clientSocket.IsConnected())
                {
                    _signal.waitForSignal();
                    reader.processMsgs();
                }
            }) { IsBackground = true }.Start();
            _messagingService.SendMessageAsync($"*IBKR Connection Established*: Connected to IBKR at {host}:{port} with ClientId {clientId}").Wait();
        }

        public void Disconnect()
        {
            if (_clientSocket.IsConnected())
            {
                _logger.LogInformation("Disconnecting from IBKR.");
                _clientSocket.eDisconnect();
            }
        }

        public int GetNextOrderId()
        {
            return _nextOrderId++;
        }

        public override void nextValidId(int orderId)
        {
            _nextOrderId = orderId;
            _logger.LogInformation($"Next valid order ID: {orderId}");
            _clientSocket.reqAllOpenOrders(); // Required to populate _openOrders so we know the Contract and Order Action
        }

        public override void execDetails(int reqId, Contract contract, Execution execution)
        {
            // Execution object only contains Cumulative Quantity and specific Execution Shares, but no Remaining Quantity.
            // We log the fill here but handle the trading logic in orderStatus where we have the 'remaining' field.
            _logger.LogInformation($"Execution Details: OrderId={execution.OrderId}, ExecId={execution.ExecId}, Symbol={contract.Symbol}, Side={execution.Side}, Shares={execution.Shares}, Price={execution.Price}");
        }

        public override void orderStatus(int orderId, string status, double filled, double remaining, double avgFillPrice, int permId, int parentId, double lastFillPrice, int clientId, string whyHeld, double mktCapPrice)
        {
            _logger.LogInformation($"OrderStatus: OrderId={orderId}, Status={status}, Filled={filled}, Remaining={remaining}");
            
            if (status == "Filled" || remaining == 0)
            {
                if (_openOrders.TryGetValue(orderId, out var orderInfo))
                {
                    Task.Run(async () => 
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var tradingService = scope.ServiceProvider.GetRequiredService<Finsight.Interfaces.ITradingService>();
                        var messagingService = scope.ServiceProvider.GetRequiredService<Finsight.Interfaces.IMessagingService>();
                        var direction = orderInfo.Order.Action.Equals("BOT", StringComparison.OrdinalIgnoreCase) || orderInfo.Order.Action.Equals("BUY", StringComparison.OrdinalIgnoreCase) 
                                        ? Finsight.Enums.TradeDirection.BUY : Finsight.Enums.TradeDirection.SELL;
                        
                        await messagingService.SendMessageAsync($"*Order Executed (IBKR)*: {orderInfo.Order.Action} {filled} shares of {orderInfo.Contract.Symbol} at Avg Price ${avgFillPrice}");
                        
                        var config = await tradingService.GetTradingConfigAsync();
                        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var defaultUserId = config?.DefaultUserId ?? (await dbContext.Users.FirstOrDefaultAsync())?.Id ?? "system";
                        
                        var trade = new Finsight.Models.FSTrade
                        {
                            Id = Guid.NewGuid(),
                            FSUserId = defaultUserId,
                            Ticker = orderInfo.Contract.Symbol,
                            TradeDirection = direction,
                            TradePrice = (decimal)avgFillPrice,
                            Quantity = (decimal)filled,
                            Date = DateTime.UtcNow,
                            ExternalId = orderId.ToString(),
                            Commission = 0
                        };
                        
                        await tradingService.HandleTradeExecutionAsync(trade);
                    });
                    _openOrders.TryRemove(orderId, out _);
                }
                else
                {
                    _logger.LogWarning($"Order {orderId} was completely filled, but its details were not found in local cache.");
                }
            }
            else if (status == "Cancelled" || status == "Inactive")
            {
                _openOrders.TryRemove(orderId, out _);
            }
        }

        public override void openOrder(int orderId, Contract contract, Order order, OrderState orderState)
        {
            _openOrders[orderId] = (contract, order);
            _logger.LogInformation($"Open Order: {orderId} {order.Action} {order.TotalQuantity} {contract.Symbol} @ {order.LmtPrice}");
        }

        public override void openOrderEnd()
        {
            _logger.LogInformation("Finished receiving open orders.");
        }

        public override void connectionClosed()
        {
            _logger.LogWarning("IBKR Connection Closed.");
        }
    }
}
