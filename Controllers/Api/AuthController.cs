using Finsight.Commands;
using Finsight.Interfaces;
using Microsoft.AspNetCore.Mvc;

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
                return BadRequest(ex.Message);
            }
        }
    }
}