using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Finsight.Commands;
using Finsight.Models;

namespace Finsight.Controller
{
    [ApiController]
    [Route("api/webhooks/bank-email")]
    public class WebhookEmailController(IDbContextFactory<AppDbContext> dbFactory) : ControllerBase
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

        [HttpPost]
        public async Task<IActionResult> ReceiveEmail([FromBody] WebhookEmailCommand payload)
        {
            await using var _context = await _dbFactory.CreateDbContextAsync();

            // 1. Get the alias from the 'Recipients' list (e.g., "b2819fa8-...")
            var recipient = payload.Recipients.FirstOrDefault();
            var externalAlias = recipient?.Split('@')[0];

            if (string.IsNullOrEmpty(externalAlias))
            {
                return Ok(); // Early return if we can't parse a valid Guid
            }

            // 2. Verify the user exists
            var userExists = await _context.Users.AnyAsync(u => u.Id == externalAlias);
            if (!userExists) return Ok();

            // 3. Map the complex Webhook Command to your flat Database Model
            var emailRecord = new FSTransactionEmail
            {
                UserId = externalAlias,
                Subject = payload.Subject ?? string.Empty,
                Text = payload.Text ?? string.Empty,
                Html = payload.Html ?? string.Empty,
                // Flatten the nested object lists into simple string lists for the DB
                From = payload.From?.Value?.Select(v => v.Address).ToList() ?? [],
                To = payload.Recipients ?? []
            };

            _context.FSTransactionEmails.Add(emailRecord);
            await _context.SaveChangesAsync();

            // 4. Fire and Forget the LLM process (Optional next step)
            // _ = Task.Run(() => ProcessEmailWithLLM(emailRecord.Id));

            return Ok();
        }
    }
}