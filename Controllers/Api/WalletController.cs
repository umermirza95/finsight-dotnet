using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Finsight.Commands;
using Finsight.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finsight.Controllers.Api
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(AuthenticationSchemes = "JwtBearer")]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateWalletAsync([FromBody] CreateWalletCommand command)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            try
            {
                var wallet = await _walletService.CreateWalletAsync(command, userIdString);
                
                return Ok(new
                {
                    message = "Wallet created successfully",
                    data = new
                    {
                        wallet.Id,
                        wallet.Name,
                        wallet.FSCurrencyCode,
                        wallet.CreationDate,
                        wallet.InitialBalance
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating the wallet." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetWalletsAsync()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            try
            {
                var wallets = await _walletService.GetWalletsAsync(userIdString);
                return Ok(new { data = wallets });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while fetching the wallets." });
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetWalletAsync(Guid id)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            try
            {
                var wallet = await _walletService.GetWalletAsync(id, userIdString);
                if (wallet == null)
                {
                    return NotFound(new { error = "Wallet not found." });
                }
                return Ok(new { data = wallet });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while fetching the wallet." });
            }
        }
    }
}
