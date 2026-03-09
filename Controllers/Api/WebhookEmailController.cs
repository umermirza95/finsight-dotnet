using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Finsight.Commands;
using Microsoft.EntityFrameworkCore;
using Finsight.Models;


namespace Finsight.Controller
{
    [ApiController]
    [Route("api/webhooks/bank-email")]
    public class WebhookEmailController(IDbContextFactory <AppDbContext> dbFactory) : ControllerBase
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;
        [HttpPost]
        public async Task<IActionResult> ReceiveEmail([FromBody] WebhookEmailCommand payload)
        {
            using var _context = await _dbFactory.CreateDbContextAsync();
        
            var recipient = payload.To.FirstOrDefault();
            var externalAlias = recipient?.Split('@')[0];

            var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == externalAlias);

            if (dbUser == null) return Ok(); 

            var emailRecord = new FSTransactionEmail
            {
                UserId = dbUser.Id,
                Subject = payload.Subject,
                Text = payload.Text,
                Html = payload.Html,
                From = payload.From,
                To = payload.To
            };

            _context.FSTransactionEmails.Add(emailRecord);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}