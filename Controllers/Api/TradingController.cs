using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Finsight.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finsight.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "JwtBearer")]
    public class TradingController : ControllerBase
    {
        private readonly ITradingService _tradingService;

        public TradingController(ITradingService tradingService)
        {
            _tradingService = tradingService;
        }

        [HttpPost("sync")]
        public async Task<IActionResult> SyncMonthlyTradesAsync()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                await _tradingService.FetchMonthlyTradesAsync(userId);
                return Ok(new { message = "Trades synchronized successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


        [HttpGet("ibkr-test")]

        public async Task<IActionResult> Test()

        {

            try
            {
                using var client = new HttpClient();

                var response = await client.GetAsync("https://gdcdyn.interactivebrokers.com/Universal/servlet/FlexStatementService.SendRequest?t=490702090196276288481712&q=1519436&v=3");

                return Ok(response.StatusCode);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine(ex.InnerException);
                Console.WriteLine(ex.InnerException?.InnerException);
                return StatusCode(500, new { error = ex.Message });
            }

        }
    }
}
