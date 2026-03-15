using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Finsight.Commands;
using Finsight.Models;
using Finsight.Interfaces;
using Finsight.Utils;
using System.Threading.Tasks;

namespace Finsight.Controller
{
    [ApiController]
    [Route("api/webhooks/bank-email")]
    public class WebhookEmailController(IDbContextFactory<AppDbContext> dbFactory, ILLMService llmService, ILogger<WebhookEmailController> logger) : ControllerBase
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;
        private readonly ILLMService _llmService = llmService;
        private readonly ILogger<WebhookEmailController> _logger = logger;

        [HttpPost("test")]
        public async Task<IActionResult> TestEndpoint([FromBody] TestPayload payload)
        {
            try
            {
                var emailRecord = new FSTransactionEmail
                {
                    UserId = "06fa3f2a-523e-42d9-a039-8eda7b0ea9fb",
                    Subject =  string.Empty,
                    Text =  string.Empty,
                    Html = EmailCleaner.CleanEmailHtml(payload.Email ?? string.Empty),
                    From =  [],
                    To =  []
                };
                var suggestion = await _llmService.CreateTransactionSuggestionAsync(emailRecord);
                return Ok(suggestion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing test email");
                return StatusCode(500, ex.Message);
            }
        }

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

            _ = Task.Run(async () => _llmService.CreateTransactionSuggestionAsync(emailRecord));

            return Ok();
        }
    }
}


public class TestPayload
{
    public string Email { get; set; }
}