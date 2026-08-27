using System.Collections.Concurrent;
using Finsight.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Finsight.Services.IBKR
{
    public interface IIBKRConnectionManager
    {
        IBKRConnectionHandler GetOrCreateHandler(string userId);
        IBKRConnectionHandler? GetHandler(string userId);
        void RemoveHandler(string userId);
    }

    public class IBKRConnectionManager : IIBKRConnectionManager
    {
        private readonly ConcurrentDictionary<string, IBKRConnectionHandler> _handlers = new();
        private readonly ILoggerFactory _loggerFactory;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMessagingService _messagingService;

        public IBKRConnectionManager(ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory, IMessagingService messagingService)
        {
            _loggerFactory = loggerFactory;
            _scopeFactory = scopeFactory;
            _messagingService = messagingService;
        }

        public IBKRConnectionHandler GetOrCreateHandler(string userId)
        {
            return _handlers.GetOrAdd(userId, id => 
            {
                var logger = _loggerFactory.CreateLogger<IBKRConnectionHandler>();
                return new IBKRConnectionHandler(logger, _scopeFactory, _messagingService, id);
            });
        }

        public IBKRConnectionHandler? GetHandler(string userId)
        {
            _handlers.TryGetValue(userId, out var handler);
            return handler;
        }

        public void RemoveHandler(string userId)
        {
            if (_handlers.TryRemove(userId, out var handler))
            {
                handler.Disconnect();
                handler.Dispose();
            }
        }
    }
}
