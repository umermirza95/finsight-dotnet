
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
    public class BudgetController(IBudgetService budgetService) : ControllerBase
    {
        private readonly IBudgetService _budgetService = budgetService;

        [HttpPost]
        public async Task<IActionResult> CreateBudgetAsync([FromBody] CreateBudgetCommand command)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var budget = await _budgetService.CreateBudgetAsync(command, userIdString);
            return Ok(new
            {
                data = budget
            });
        }
        
    }
}