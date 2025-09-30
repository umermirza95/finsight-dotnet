using Microsoft.AspNetCore.Mvc;

namespace Finsight.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register()
        {
            return Ok(new { Message = "User registered successfully!" });
        }
    }
}