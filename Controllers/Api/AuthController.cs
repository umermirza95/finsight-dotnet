using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Finsight.Commands;
using Finsight.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Finsight.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {

        private readonly IFSUserService _userService;


        public AuthController(IFSUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] CreateFSUserCommand command)
        {
            try
            {
                var user = await _userService.SignUp(command);
                return Ok(user);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = ex.Message
                });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginFSUserCommand command)
        {
            try
            {
                var user = await _userService.Login(command);
                var token = _userService.GenerateToken(user);

                return Ok(new
                {
                    token
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = ex.Message
                });
            }
        }

        [HttpPost("login-web")]
        public async Task<IActionResult> LoginWeb([FromForm] LoginFSUserCommand command)
        {
            try
            {
                var user = await _userService.Login(command);
                var claims = new List<Claim>
                {
                    new(ClaimTypes.Name, user.UserName ?? ""),
                    new(ClaimTypes.NameIdentifier, user.Id),
                    new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                var identity = new ClaimsIdentity(claims, "Cookies");
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync("Cookies", principal);

                return Redirect("/dashboard");
            }
            catch (Exception ex)
            {
                
                return Redirect("/login?error=" + Uri.EscapeDataString(ex.Message));
            }
        }
    }
}