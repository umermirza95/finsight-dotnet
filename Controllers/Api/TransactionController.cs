using System.Security.Claims;
using Finsight.Commands;
using Finsight.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finsight.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "JwtBearer")]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpPost]
        public async Task<IActionResult> AddTransactionAsync([FromBody] CreateTransactionCommand command)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var transaction = await _transactionService.AddTransactionAsync(command, userIdString);
            return CreatedAtAction( nameof(AddTransactionAsync), new
            {
                data = transaction
            });
        }
    }
}