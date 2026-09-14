using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Finsight.Enums;
using Finsight.Interfaces;
using Finsight.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Finsight.Services
{
    public class FSNotificationService : IFSNotificationService
    {
        private readonly AppDbContext _dbContext;
        private readonly IPushNotificationService _pushNotificationService;
        private readonly ILogger<FSNotificationService> _logger;

        public FSNotificationService(
            AppDbContext dbContext,
            IPushNotificationService pushNotificationService,
            ILogger<FSNotificationService> logger)
        {
            _dbContext = dbContext;
            _pushNotificationService = pushNotificationService;
            _logger = logger;
        }

        public async Task<FSNotification?> GetByIdAsync(Guid id)
        {
            return await _dbContext.FSNotifications.FindAsync(id);
        }

        public async Task<IEnumerable<FSNotification>> GetAllForUserAsync(string userId)
        {
            return await _dbContext.FSNotifications
                .Where(n => n.FSUserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task CreateTradeNotificationAsync(FSTrade trade)
        {
            try
            {
                string title;
                string body;

                if (trade.TradeDirection == TradeDirection.BUY)
                {
                    title = "BUY Trade Executed";
                    body = $"Bought {trade.Quantity} shares of {trade.Ticker} at ${trade.TradePrice:F2}.";
                }
                else
                {
                    title = "SELL Trade Executed";
                    body = $"Sold {trade.Quantity} shares of {trade.Ticker} at ${trade.TradePrice:F2}.";
                }

                var notification = new FSNotification
                {
                    Id = Guid.NewGuid(),
                    FSUserId = trade.FSUserId,
                    Title = title,
                    Message = body,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    Type = FSNotificationType.Trade
                };

                _dbContext.FSNotifications.Add(notification);
                await _dbContext.SaveChangesAsync();

                var notificationData = new
                {
                    title = title,
                    body = body,
                    url = "/"
                };
                var jsonPayload = System.Text.Json.JsonSerializer.Serialize(notificationData);

                await _pushNotificationService.SendNotificationAsync(trade.FSUserId, jsonPayload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating trade notification for trade {TradeId}", trade.Id);
            }
        }

        public async Task MarkAsReadAsync(Guid id)
        {
            var notification = await _dbContext.FSNotifications.FindAsync(id);
            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            var notification = await _dbContext.FSNotifications.FindAsync(id);
            if (notification != null)
            {
                _dbContext.FSNotifications.Remove(notification);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
