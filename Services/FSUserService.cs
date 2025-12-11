using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Finsight.Commands;
using Finsight.Constants;
using Finsight.Interfaces;
using Finsight.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Finsight.Services
{
    public class FSUserService : IFSUserService
    {
        private readonly UserManager<FSUser> _userManager;
        private readonly IConfiguration _config;

        public FSUserService(UserManager<FSUser> userManager, IConfiguration config)
        {
            _userManager = userManager;
            _config = config;
        }
        public async Task<FSUser> SignUp(CreateFSUserCommand command)
        {
            var user = new FSUser
            {
                Email = command.Email,
                UserName = command.Name
            };
            var result = await _userManager.CreateAsync(user, command.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new ApplicationException(errors);
            }
            return user;
        }

        public async Task<FSUser> Login(LoginFSUserCommand command)
        {
            var user = await _userManager.FindByEmailAsync(command.Email) ?? throw new ApplicationException(FSErrors.invalidEmailOrPassword);
            if (!await _userManager.CheckPasswordAsync(user, command.Password))
            {
                throw new ApplicationException(FSErrors.invalidEmailOrPassword);
            }
            return user;
        }

        public string GenerateToken(FSUser user)
        {
            var authClaims = new List<Claim>
                {
                    new(ClaimTypes.Name, user.UserName),
                    new(ClaimTypes.NameIdentifier, user.Id),
                    new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                expires: DateTime.Now.AddHours(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}