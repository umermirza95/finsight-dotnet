
using System.Net.Mime;
using System.Security.Claims;
using Finsight.Command;
using Finsight.Interface;
using Finsight.Models;
using Finsight.Query;
using Finsight.Repositories;
using Finsight.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finsight.Controller
{
    [ApiController]
    [Authorize]
    [Route("api/transaction")]
    public class FSTransactionController : ControllerBase
    {
        readonly FSTransactionService transactionService;
        readonly FSTransactionRepository transactionRepository;
        public FSTransactionController(FSTransactionService transactionService, FSTransactionRepository transactionRepository)
        {
            this.transactionService = transactionService;
            this.transactionRepository = transactionRepository;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IAsyncEnumerable<FSTransactionModel> GetTransactionsAsync([FromQuery] FSTransactionQuery query)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            return transactionRepository.FetchAsync(userId, query);
        }


        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<FSTransactionModel>> CreateTransaction([FromBody] FSCreateTransactionCommand command)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            var transaction = await transactionService.CreateTransactionAsync(userId, command);
            return CreatedAtAction("transaction", transaction);
        }
    }
}