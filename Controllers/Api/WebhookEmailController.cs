using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Finsight.Commands;
using Finsight.Models;
using Finsight.Interfaces;
using Finsight.Utils;

namespace Finsight.Controller
{
    [ApiController]
    [Route("api/webhooks/bank-email")]
    public class WebhookEmailController(IDbContextFactory<AppDbContext> dbFactory, ILLMService llmService, ILogger<WebhookEmailController> logger) : ControllerBase
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;
        private readonly ILLMService _llmService = llmService;
        private readonly ILogger<WebhookEmailController> _logger = logger;

       

        [HttpPost]
        public async Task<IActionResult> ReceiveEmail([FromBody] WebhookEmailCommand payload)
        {
            
            await using var _context = await _dbFactory.CreateDbContextAsync();

            var recipient = payload.Recipients.FirstOrDefault();
            var externalAlias = recipient?.Split('@')[0];

            if (string.IsNullOrEmpty(externalAlias))
            {
                return Ok();
            }


            var userExists = await _context.Users.AnyAsync(u => u.Id == externalAlias);
            if (!userExists) return Ok();


            var emailRecord = new FSTransactionEmail
            {
                UserId = externalAlias,
                Subject = payload.Subject ?? string.Empty,
                Text = payload.Text ?? string.Empty,
                Html = EmailCleaner.CleanEmailHtml(payload.Html ?? string.Empty),
                From = payload.From?.Value?.Select(v => v.Address).ToList() ?? [],
                To = payload.Recipients ?? []
            };

            _context.FSTransactionEmails.Add(emailRecord);
            await _context.SaveChangesAsync();

            _ = Task.Run(async () => _llmService.CreateTransactionSuggestionAsync(emailRecord.Html, externalAlias));

            return Ok();
        }
    }
}