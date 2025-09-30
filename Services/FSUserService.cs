using Finsight.Commands;
using Finsight.Interfaces;
using Finsight.Models;
using Microsoft.AspNetCore.Identity;

namespace Finsight.Services
{
    public class FSUserService : IFSUserService
    {
        private readonly UserManager<FSUser> _userManager;

        public FSUserService(UserManager<FSUser> userManager)
        {
            _userManager = userManager;
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
    }
}