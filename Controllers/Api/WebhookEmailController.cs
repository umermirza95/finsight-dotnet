using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Finsight.Commands;
using Finsight.Models;
using Finsight.Interfaces;

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
        public async Task<IActionResult> TestEndpoint([FromBody] WebhookEmailCommand payload)
        {
            try
            {
                await _llmService.CreateTransactionSuggestionAsync(payload, "06fa3f2a-523e-42d9-a039-8eda7b0ea9fb");
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

        }

        [HttpPost]
        public async Task<IActionResult> ReceiveEmail()
        {
            Request.EnableBuffering();
            using var reader = new StreamReader(Request.Body, leaveOpen: true);
            var rawJson = await reader.ReadToEndAsync();
            var payloadPath = Path.Combine(Directory.GetCurrentDirectory(), "last_webhook_payload.json");
            await System.IO.File.WriteAllTextAsync(payloadPath, rawJson);
            var payload = System.Text.Json.JsonSerializer.Deserialize<WebhookEmailCommand>(rawJson, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (payload == null)
            {
                _logger.LogError("Failed to deserialize webhook payload. Raw JSON: {RawJson}", rawJson);
                return Ok();
            }

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
                Html = payload.Html ?? string.Empty,
                From = payload.From?.Value?.Select(v => v.Address).ToList() ?? [],
                To = payload.Recipients ?? []
            };

            _context.FSTransactionEmails.Add(emailRecord);
            await _context.SaveChangesAsync();

            //_ = Task.Run(async () => _llmService.CreateTransactionSuggestionAsync(payload, externalAlias));

            return Ok();
        }
    }
}