using System;
using System.Linq;
using System.Threading.Tasks;
using Finsight.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebPush;

namespace Finsight.Services
{
    public class FSPushNotificationService : IPushNotificationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<FSPushNotificationService> _logger;
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly WebPushClient _webPushClient;
        private readonly VapidDetails? _vapidDetails;

        public FSPushNotificationService(IConfiguration configuration, ILogger<FSPushNotificationService> logger, IDbContextFactory<AppDbContext> dbFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _dbFactory = dbFactory;
            _webPushClient = new WebPushClient();

            var subject = _configuration["Vapid:Subject"];
            var publicKey = _configuration["Vapid:PublicKey"];
            var privateKey = _configuration["Vapid:PrivateKey"];

            if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(privateKey))
            {
                _logger.LogWarning("VAPID details are not fully configured in appsettings.json. Push notifications may fail.");
            }
            else
            {
                _vapidDetails = new VapidDetails(subject, publicKey, privateKey);
            }
        }

        public async Task SendNotificationAsync(string userId, string payload)
        {
            if (_vapidDetails == null)
            {
                _logger.LogError("Cannot send push notification. VAPID details are missing.");
                return;
            }

            using var context = await _dbFactory.CreateDbContextAsync();
            var subscriptions = await context.FSPushSubscriptions
                .Where(s => s.FSUserId == userId)
                .ToListAsync();

            if (subscriptions.Count == 0)
            {
                _logger.LogInformation("No push subscriptions found for user {UserId}", userId);
                return;
            }

            foreach (var sub in subscriptions)
            {
                var pushSubscription = new PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);

                try
                {
                    await _webPushClient.SendNotificationAsync(pushSubscription, payload, _vapidDetails);
                    _logger.LogInformation("Push notification sent successfully to endpoint {Endpoint}", sub.Endpoint);
                }
                catch (WebPushException ex)
                {
                    // 410 Gone means the subscription is no longer valid (user unsubscribed or token expired)
                    if (ex.StatusCode == System.Net.HttpStatusCode.Gone)
                    {
                        _logger.LogWarning("Push subscription {Endpoint} is no longer valid. Removing from database.", sub.Endpoint);
                        context.FSPushSubscriptions.Remove(sub);
                    }
                    else
                    {
                        _logger.LogError(ex, "Failed to send push notification. Status code: {StatusCode}", ex.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An unexpected error occurred while sending a push notification.");
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
