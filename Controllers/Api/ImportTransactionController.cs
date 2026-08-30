using Finsight.DTOs;
using Finsight.Interfaces;
using Finsight.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Finsight.Controllers.Api
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(AuthenticationSchemes = "JwtBearer")]
    public class ImportTransactionController : ControllerBase
    {
        private readonly ILLMService _llmService;
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public ImportTransactionController(ILLMService llmService, IDbContextFactory<AppDbContext> dbFactory)
        {
            _llmService = llmService;
            _dbFactory = dbFactory;
        }

        private string GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadBankStatement([FromForm] IFormFile file, [FromForm] Guid walletId)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            if (walletId == Guid.Empty)
            {
                return BadRequest("Wallet ID is required.");
            }

            if (file.ContentType != "application/pdf")
            {
                return BadRequest("Only PDF files are supported.");
            }

            var userId = GetUserId();
            using var stream = file.OpenReadStream();
            var transactions = await _llmService.ParseBankStatementAsync(stream, userId, walletId);

            var dtos = transactions.Select(t => new FSImportedTransactionDTO
            {
                Id = t.Id,
                Description = t.Description,
                Amount = t.Amount,
                Date = t.Date,
                BankName = t.BankName,
                FSCurrencyCode = t.FSCurrencyCode,
                Type = t.Type
            }).ToList();

            return Ok(new { data = new { transactions = dtos } });
        }

        [HttpGet("imported")]
        public async Task<IActionResult> GetImportedTransactions()
        {
            var userId = GetUserId();
            using var context = await _dbFactory.CreateDbContextAsync();

            var transactions = await context.FSImportedTransactions
                .Where(t => t.FSUserId == userId && !t.IsDeleted && t.FSTransactionId == null)
                .OrderByDescending(t => t.Date)
                .ToListAsync();

            var dtos = transactions.Select(t => new FSImportedTransactionDTO
            {
                Id = t.Id,
                Description = t.Description,
                Amount = t.Amount,
                Date = t.Date,
                BankName = t.BankName,
                FSCurrencyCode = t.FSCurrencyCode,
                Type = t.Type
            }).ToList();

            return Ok(new { data = new { transactions = dtos } });
        }

        [HttpDelete("imported/{id}")]
        public async Task<IActionResult> DeleteImportedTransaction(string id)
        {
            var userId = GetUserId();
            using var context = await _dbFactory.CreateDbContextAsync();

            var tx = await context.FSImportedTransactions
                .FirstOrDefaultAsync(t => t.Id == id && t.FSUserId == userId);

            if (tx == null)
            {
                return NotFound();
            }

            tx.IsDeleted = true;
            await context.SaveChangesAsync();

            return Ok(new { success = true });
        }
        
        [HttpPost("imported/delete-multiple")]
        public async Task<IActionResult> DeleteMultipleImportedTransactions([FromBody] List<string> ids)
        {
            var userId = GetUserId();
            using var context = await _dbFactory.CreateDbContextAsync();

            var txs = await context.FSImportedTransactions
                .Where(t => ids.Contains(t.Id) && t.FSUserId == userId)
                .ToListAsync();

            foreach(var tx in txs)
            {
                tx.IsDeleted = true;
            }
            
            await context.SaveChangesAsync();

            return Ok(new { success = true });
        }

    }
}
