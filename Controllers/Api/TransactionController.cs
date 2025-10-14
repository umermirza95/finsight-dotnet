using System.Security.Claims;
using Finsight.Commands;
using Finsight.Interfaces;
using Finsight.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finsight.Controller
{
    [ApiController]
    [Route("api/[controller]")]

    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTransactionsAsync([FromQuery] GetTransactionsQuery query)
        {
            query.ApplyDefaultDateRange();
            var userIdString = "b2819fa8-5207-4dff-ab65-7ac14a42663b"; // User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var transactions = await _transactionService.GetTransactionsAsync(query, userIdString);
            return Ok(new { data = new { transactions } });
        }

        [HttpPost]
        public async Task<IActionResult> AddTransactionAsync([FromBody] CreateTransactionCommand command)
        {
            var userIdString = "b2819fa8-5207-4dff-ab65-7ac14a42663b";// User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var transaction = await _transactionService.AddTransactionAsync(command, userIdString);
            return Ok(new
            {
                data = transaction
            });
        }
    }
}