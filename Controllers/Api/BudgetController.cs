
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

        [HttpGet("{budgetId:guid}")]
        public async Task<IActionResult> GetBudgetByIdAsync([FromRoute] Guid budgetId, [FromQuery] DateOnly? referenceDate)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                // Pass the optional referenceDate to the service
                var budget = await _budgetService.GetBudgetByIdAsync(budgetId, userIdString!, referenceDate);

                return Ok(new
                {
                    data = budget
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveBudgetsAsync([FromQuery] DateOnly? referenceDate)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var budgets = await _budgetService.GetActiveBudgetsAsync(userIdString!, referenceDate);
            var totalBudgetAmount = budgets.Sum(b => b.TotalAmount);
            var totalConsumedAmount = budgets.Sum(b => b.ConsumedAmount);
            return Ok(new
            {
                summary = new
                {
                    TotalBudgetAmount = totalBudgetAmount,
                    TotalConsumedAmount = totalConsumedAmount,
                    RemainingAmount = totalBudgetAmount - totalConsumedAmount,
                    BudgetCount = budgets.Count
                },
               budgets
            });
        }

    }
}