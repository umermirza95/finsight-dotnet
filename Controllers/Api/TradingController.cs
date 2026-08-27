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
    [Route("[controller]")]
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

                await _tradingService.FetchTodayTradesAsync(userId);
                await _tradingService.MatchClosedTradesAsync(userId);
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


        [HttpPost("connect")]
        public async Task<IActionResult> ConnectAsync([FromBody] Commands.ConnectCommand command, [FromServices] IBrokerService brokerService)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                if (command.Connect)
                {
                    var config = await _tradingService.GetTradingConfigAsync(userId);
                    if (config == null || string.IsNullOrEmpty(config.ServerIp))
                    {
                        return BadRequest(new { error = "Trading config or Server IP is not set for this user." });
                    }

                    var host = config.ServerIp;
                    var port = 7497; // default port

                    if (config.ServerIp.Contains(':'))
                    {
                        var parts = config.ServerIp.Split(':');
                        host = parts[0];
                        if (int.TryParse(parts[1], out int parsedPort))
                        {
                            port = parsedPort;
                        }
                    }

                    await brokerService.ConnectAsync(host, port, 1234, userId);
                }
                else
                {
                    brokerService.Disconnect(userId);
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("status")]
        public IActionResult GetStatus([FromServices] IBrokerService brokerService)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                return Ok(new { isConnected = brokerService.IsConnected(userId) });
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
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var config = await _tradingService.GetTradingConfigAsync(userId);
                return Ok(new { config = config, isConnected = brokerService.IsConnected(userId) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("config")]
        public async Task<IActionResult> UpdateConfigAsync([FromBody] Commands.UpdateTradingConfigCommand dto)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var config = await _tradingService.UpdateTradingConfigAsync(userId, dto);
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
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var orders = await brokerService.GetActiveOrdersAsync(userId);
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
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var direction = request.Direction.Equals("BUY", StringComparison.OrdinalIgnoreCase) 
                                ? Finsight.Enums.TradeDirection.BUY : Finsight.Enums.TradeDirection.SELL;
                                
                await brokerService.PlaceLimitOrderAsync(userId, request.Ticker, direction, request.LimitPrice, request.Quantity, false, request.Account);
                
                var orders = await brokerService.GetActiveOrdersAsync(userId);
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
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                await brokerService.AdjustOrderPriceAsync(userId, request.PermId, request.NewPrice);
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
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                await brokerService.CancelOrderAsync(userId, orderId);
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
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                await brokerService.CancelAllOrdersAsync(userId, false);
                return Ok(new { message = "All orders cancelled successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("profit-distribution")]
        public async Task<IActionResult> MakeProfitDistributionAsync([FromBody] Commands.MakeProfitDistributionCommand command)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                await _tradingService.MakeProfitDistributionAsync(userId, command);
                return Ok(new { message = "Profit distribution recorded successfully." });
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

        [HttpGet("available-balance")]
        public async Task<IActionResult> GetAvailableBalanceAsync()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var balance = await _tradingService.GetAvailableBalanceAsync(userId);
                return Ok(new { availableBalance = balance });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("profit-distributions")]
        public async Task<IActionResult> GetProfitDistributionsAsync([FromQuery] GetProfitDistributionsQuery query)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var distributions = await _tradingService.GetProfitDistributionsAsync(userId, query);
                return Ok(distributions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("insurance-payouts")]
        public async Task<IActionResult> GetInsurancePayoutsAsync([FromQuery] GetInsurancePayoutsQuery query)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var payouts = await _tradingService.GetInsurancePayoutsAsync(userId, query);
                return Ok(payouts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("insurance-balance")]
        public async Task<IActionResult> GetInsuranceBalanceAsync()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var balance = await _tradingService.GetInsuranceBalanceAsync(userId);
                return Ok(new { insuranceBalance = balance });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
        [HttpGet("reconcile")]
        public async Task<IActionResult> GetReconciliationAsync()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var difference = await _tradingService.ReconcileBalanceWithBrokerAsync(userId);
                return Ok(new { difference = difference });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
