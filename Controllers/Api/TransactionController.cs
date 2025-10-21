using System.Security.Claims;
using Finsight.Commands;
using Finsight.DTOs;
using Finsight.Interfaces;
using Finsight.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finsight.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "JwtBearer")]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionsController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTransactionsAsync([FromQuery] GetTransactionsQuery query)
        {
            query.ApplyDefaultDateRange();
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var transactions = await _transactionService.GetTransactionsInDefaultCurrencyAsync(query, userIdString);

            return Ok(new { data = new { transactions } });
        }

        [HttpPost]
        public async Task<IActionResult> AddTransactionAsync([FromBody] CreateTransactionCommand command)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var transaction = await _transactionService.AddTransactionWithFXAsync(command, userIdString);
            return Ok(new
            {
                data = transaction
            });
        }


        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteTransactionAsync(Guid id)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var deleted = await _transactionService.DeleteTransactionAsync(id, userIdString);

            if (!deleted)
            {
                return NotFound(new
                {
                    error = "Transaction not found or you do not have permission to delete it."
                });
            }

            return Ok(new
            {
                message = "Transaction deleted successfully."
            });
        }
    }
}