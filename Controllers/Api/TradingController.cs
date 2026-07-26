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
        public async Task<IActionResult> UpdateConfigAsync([FromBody] Commands.UpdateTradingConfigCommand dto, [FromServices] IBrokerService brokerService)
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

        [HttpGet("active-orders")]
        public async Task<IActionResult> GetActiveOrdersAsync([FromServices] IBrokerService brokerService)
        {
            try
            {
                var orders = await brokerService.GetActiveOrdersAsync();
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("active-orders/place-order")]
        public async Task<IActionResult> PlaceOrderAsync([FromBody] Commands.PlaceOrderCommand request, [FromServices] IBrokerService brokerService)
        {
            try
            {
                var direction = request.Direction.Equals("BUY", StringComparison.OrdinalIgnoreCase) 
                                ? Finsight.Enums.TradeDirection.BUY : Finsight.Enums.TradeDirection.SELL;
                                
                await brokerService.PlaceLimitOrderAsync(request.Ticker, direction, request.LimitPrice, request.Quantity, logsOnly: false);
                
                var orders = await brokerService.GetActiveOrdersAsync();
                return Ok(new { message = "Order placed successfully.", orders = orders });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("active-orders/adjust-price")]
        public async Task<IActionResult> AdjustOrderPriceAsync([FromBody] Commands.AdjustOrderPriceCommand request, [FromServices] IBrokerService brokerService)
        {
            try
            {
                await brokerService.AdjustOrderPriceAsync(request.PermId, request.NewPrice);
                return Ok(new { message = "Order price adjusted successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("manual-match")]
        public async Task<IActionResult> ManualMatchAsync([FromBody] Commands.ManualMatchCommand command)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                await _tradingService.ManualMatchTradesAsync(userId, command);
                return Ok(new { message = "Trades matched successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("active-orders/{orderId}")]
        public async Task<IActionResult> CancelOrderAsync(int orderId, [FromServices] IBrokerService brokerService)
        {
            try
            {
                await brokerService.CancelOrderAsync(orderId);
                return Ok(new { message = "Order cancelled successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("active-orders")]
        public async Task<IActionResult> CancelAllOrdersAsync([FromServices] IBrokerService brokerService)
        {
            try
            {
                await brokerService.CancelAllOrdersAsync(false);
                return Ok(new { message = "All orders cancelled successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
