
using System.Security.Claims;
using Finsight.Interface;
using Finsight.Models;
using Finsight.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finsight.Controller
{
    [ApiController]
    [Authorize]
    [Route("api/transaction")]
    public class FSTransactionController : ControllerBase
    {
        FSITransactionRepository transactionRepository;
        public FSTransactionController(FSITransactionRepository transactionRepository)
        {
            this.transactionRepository = transactionRepository;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IAsyncEnumerable<FSTransactionModel> GetTransactionsAsync([FromQuery] FSTransactionQuery query)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            return transactionRepository.FetchAsync(userId, query);

        }
    }
}