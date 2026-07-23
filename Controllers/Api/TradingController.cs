using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Finsight.Interfaces;
using Finsight.Queries;
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

        [HttpPost("match")]
        public async Task<IActionResult> MatchTradesAsync()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                await _tradingService.MatchClosedTradesAsync(userId);
                return Ok(new { message = "Trades matched and closed successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("open")]
        public async Task<IActionResult> GetOpenTradesAsync()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var openTrades = await _tradingService.GetOpenTradesAsync(userId);
                return Ok(openTrades);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("closed")]
        public async Task<IActionResult> GetClosedTradesAsync([FromQuery] GetTradesQuery query)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var closedTrades = await _tradingService.GetClosedTradesAsync(userId, query);
                return Ok(closedTrades);
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
        [HttpGet("config")]
        public async Task<IActionResult> GetConfigAsync([FromServices] IBrokerService brokerService)
        {
            try
            {
                var config = await _tradingService.GetTradingConfigAsync();
                return Ok(new { config = config, isConnected = brokerService.IsConnected });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("config")]
        public async Task<IActionResult> UpdateConfigAsync([FromBody] DTOs.UpdateTradingConfigDTO dto, [FromServices] IBrokerService brokerService)
        {
            try
            {
                var previousConfig = await _tradingService.GetTradingConfigAsync();
                bool wasAutoTradeOn = previousConfig?.AutoTrade ?? false;

                var config = await _tradingService.UpdateTradingConfigAsync(dto);

                if (dto.AutoTrade.HasValue && dto.AutoTrade.Value != wasAutoTradeOn)
                {
                    if (dto.AutoTrade.Value)
                    {
                        brokerService.Connect();
                    }
                    else
                    {
                        brokerService.Disconnect();
                    }
                }

                return Ok(config);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
