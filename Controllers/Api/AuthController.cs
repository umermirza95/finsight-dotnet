using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Finsight.Commands;
using Finsight.Interfaces;
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
    }
}