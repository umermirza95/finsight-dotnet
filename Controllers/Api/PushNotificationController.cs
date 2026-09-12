using System.Security.Claims;
using System.Threading.Tasks;
using Finsight.DTOs;
using Finsight.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Finsight.Controllers.Api
{
    [ApiController]
    [Route("api/push-notifications")]
    [Authorize]
    public class PushNotificationController : ControllerBase
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public PushNotificationController(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            using var _context = await _dbFactory.CreateDbContextAsync();

            // Check if this endpoint is already registered for this user
            var existingSubscription = await _context.FSPushSubscriptions
                .FirstOrDefaultAsync(s => s.FSUserId == userId && s.Endpoint == request.Endpoint);

            if (existingSubscription != null)
            {
                // Update keys if they somehow changed
                existingSubscription.P256dh = request.Keys.P256dh;
                existingSubscription.Auth = request.Keys.Auth;
                _context.FSPushSubscriptions.Update(existingSubscription);
            }
            else
            {
                // Create a new subscription
                var subscription = new FSPushSubscription
                {
                    FSUserId = userId,
                    Endpoint = request.Endpoint,
                    P256dh = request.Keys.P256dh,
                    Auth = request.Keys.Auth
                };
                await _context.FSPushSubscriptions.AddAsync(subscription);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Push subscription saved successfully." });
        }

        [HttpDelete("unsubscribe")]
        public async Task<IActionResult> Unsubscribe([FromQuery] string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint))
                return BadRequest("Endpoint is required.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            using var _context = await _dbFactory.CreateDbContextAsync();
            var subscription = await _context.FSPushSubscriptions
                .FirstOrDefaultAsync(s => s.FSUserId == userId && s.Endpoint == endpoint);

            if (subscription != null)
            {
                _context.FSPushSubscriptions.Remove(subscription);
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Push subscription removed successfully." });
        }
    }
}
