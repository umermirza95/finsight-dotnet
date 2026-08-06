using IBApi;
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
        private readonly System.Collections.Concurrent.ConcurrentDictionary<int, TaskCompletionSource<bool>> _pendingOrders = new();
        private readonly IMessagingService _messagingService;
        private TaskCompletionSource<bool>? _openOrdersTcs;
        
        private readonly System.Collections.Concurrent.ConcurrentDictionary<int, TaskCompletionSource<IEnumerable<(Contract Contract, Execution Execution)>>> _pendingExecutions = new();
        private readonly System.Collections.Concurrent.ConcurrentDictionary<int, List<(Contract Contract, Execution Execution)>> _executionsData = new();
        private int _nextReqId = 1;
        
        private bool _isConnected;
        public bool IsConnected => _isConnected && _clientSocket.IsConnected();

        private readonly string _userId;

        public IBKRConnectionHandler(ILogger<IBKRConnectionHandler> logger, IServiceScopeFactory scopeFactory, IMessagingService messagingService, string userId)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _messagingService = messagingService;
            _userId = userId;
            _signal = new EReaderMonitorSignal();
            _clientSocket = new EClientSocket(this, _signal);
        }

        public void Connect(string host, int port, int clientId)
        {
            if (IsConnected)
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
            _isConnected = false;
        }

        public int GetNextOrderId()
        {
            return _nextOrderId++;
        }

        public IEnumerable<(int OrderId, Contract Contract, Order Order)> GetOpenOrders()
        {
            return _openOrders.Select(kv => (kv.Key, kv.Value.Contract, kv.Value.Order));
        }

        public Task WaitForOrderPlacementAsync(int orderId, TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingOrders[orderId] = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() => 
            {
                if (_pendingOrders.TryRemove(orderId, out var pendingTcs))
                {
                    pendingTcs.TrySetException(new TimeoutException("Order placement timed out waiting for IBKR confirmation."));
                }
            });

            return tcs.Task;
        }

        public async Task<IEnumerable<(int OrderId, Contract Contract, Order Order)>> RefreshAndGetOpenOrdersAsync()
        {
            _openOrdersTcs = new TaskCompletionSource<bool>();
            _openOrders.Clear();
            
            _clientSocket.reqAllOpenOrders();
            
            var fetchTask = _openOrdersTcs.Task;
            if (await Task.WhenAny(fetchTask, Task.Delay(5000)) != fetchTask)
            {
                _logger.LogWarning("Timeout waiting for open orders from IBKR.");
            }
            
            return GetOpenOrders();
        }

        public Task<IEnumerable<(Contract Contract, Execution Execution)>> GetExecutionsAsync()
        {
            var reqId = Interlocked.Increment(ref _nextReqId);
            var tcs = new TaskCompletionSource<IEnumerable<(Contract Contract, Execution Execution)>>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingExecutions[reqId] = tcs;
            _executionsData[reqId] = new List<(Contract Contract, Execution Execution)>();

            _clientSocket.reqExecutions(reqId, new ExecutionFilter());

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            cts.Token.Register(() => 
            {
                if (_pendingExecutions.TryRemove(reqId, out var pendingTcs))
                {
                    _executionsData.TryRemove(reqId, out _);
                    pendingTcs.TrySetException(new TimeoutException("Timeout waiting for executions from IBKR."));
                }
            });

            return tcs.Task;
        }

        public override void nextValidId(int orderId)
        {
            _isConnected = true;
            _nextOrderId = orderId;
            _logger.LogInformation($"Next valid order ID: {orderId}");
            //_clientSocket.reqAutoOpenOrders(true); // Automatically bind manual/mobile orders to this client ID to receive their execution events
            _clientSocket.reqAllOpenOrders(); // Required to populate _openOrders so we know the Contract and Order Action
        }

        public override void execDetails(int reqId, Contract contract, Execution execution)
        {
            // Execution object only contains Cumulative Quantity and specific Execution Shares, but no Remaining Quantity.
            // We log the fill here but handle the trading logic in orderStatus where we have the 'remaining' field.
            _logger.LogInformation($"Execution Details: OrderId={execution.OrderId}, ExecId={execution.ExecId}, Symbol={contract.Symbol}, Side={execution.Side}, Shares={execution.Shares}, Price={execution.Price}");
            
            if (_executionsData.TryGetValue(reqId, out var list))
            {
                lock (list)
                {
                    list.Add((contract, execution));
                }
            }
        }

        public override void execDetailsEnd(int reqId)
        {
            _logger.LogInformation($"Execution Details End for ReqId={reqId}");
            if (_pendingExecutions.TryRemove(reqId, out var tcs))
            {
                if (_executionsData.TryRemove(reqId, out var data))
                {
                    tcs.TrySetResult(data);
                }
                else
                {
                    tcs.TrySetResult(new List<(Contract Contract, Execution Execution)>());
                }
            }
        }

        public override void orderStatus(int orderId, string status, double filled, double remaining, double avgFillPrice, int permId, int parentId, double lastFillPrice, int clientId, string whyHeld, double mktCapPrice)
        {
            if (_pendingOrders.TryRemove(orderId, out var tcs))
            {
                tcs.TrySetResult(true);
            }

            _logger.LogInformation($"OrderStatus: OrderId={orderId}, PermId={permId}, Status={status}, Filled={filled}, Remaining={remaining}");
            
            if (status == "Filled" || remaining == 0)
            {
                if (_openOrders.TryGetValue(permId, out var orderInfo))
                {
                    Task.Run(async () => 
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var tradingService = scope.ServiceProvider.GetRequiredService<Finsight.Interfaces.ITradingService>();
                        var messagingService = scope.ServiceProvider.GetRequiredService<Finsight.Interfaces.IMessagingService>();
                        var direction = orderInfo.Order.Action.Equals("BOT", StringComparison.OrdinalIgnoreCase) || orderInfo.Order.Action.Equals("BUY", StringComparison.OrdinalIgnoreCase) 
                                        ? Finsight.Enums.TradeDirection.BUY : Finsight.Enums.TradeDirection.SELL;
                        
                        await messagingService.SendMessageAsync($"*Order Executed (IBKR)*: {orderInfo.Order.Action} {filled} shares of {orderInfo.Contract.Symbol} at Avg Price ${avgFillPrice} ID: {permId} {parentId}");
                        
                        var trade = new Finsight.Models.FSTrade
                        {
                            Id = Guid.NewGuid(),
                            FSUserId = _userId,
                            Ticker = orderInfo.Contract.Symbol,
                            TradeDirection = direction,
                            TradePrice = (decimal)avgFillPrice,
                            Quantity = (decimal)filled,
                            Date = DateTime.UtcNow,
                            ExternalId = permId.ToString(),
                            Commission = 1
                        };
                        
                        await tradingService.HandleTradeExecutionAsync(trade);
                    });
                    _openOrders.TryRemove(permId, out _);
                }
                else
                {
                    _logger.LogWarning($"Order with PermId {permId} was completely filled, but its details were not found in local cache.");
                }
            }
            else if (status == "Cancelled" || status == "Inactive")
            {
                _openOrders.TryRemove(permId, out _);
            }
        }

        public override void openOrder(int orderId, Contract contract, Order order, OrderState orderState)
        {
            _openOrders[order.PermId] = (contract, order);
            _logger.LogInformation($"Open Order: PermId={order.PermId} {order.Action} {order.TotalQuantity} {contract.Symbol} @ {order.LmtPrice}");
            
            if (_pendingOrders.TryRemove(orderId, out var tcs))
            {
                tcs.TrySetResult(true);
            }
        }

        public override void openOrderEnd()
        {
            _logger.LogInformation("Finished receiving open orders.");
            _openOrdersTcs?.TrySetResult(true);
        }

        public override void connectionClosed()
        {
            _isConnected = false;
            _logger.LogWarning("IBKR Connection Closed.");
        }

        public override void error(int id, int errorCode, string errorMsg)
        {
            _logger.LogError($"IBKR Error [{errorCode}]: {errorMsg}");
            
            if (id != -1 && _pendingOrders.TryRemove(id, out var tcs))
            {
                tcs.TrySetException(new Exception($"IBKR Error [{errorCode}]: {errorMsg}"));
            }
            
            // 504: Not connected, 1100: Connectivity between IB and TWS has been lost, 2110: Connectivity between IB and TWS has been lost
            if (errorCode == 504 || errorCode == 1100 || errorCode == 2110)
            {
                _isConnected = false;
            }
            // 1101, 1102: Connectivity restored
            else if (errorCode == 1101 || errorCode == 1102)
            {
                _isConnected = true;
            }
        }
    }
}
