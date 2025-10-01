using Finsight.Commands;
using Finsight.Models;

namespace Finsight.Interfaces
{
    public interface IFSUserService
    {
        public Task<FSUser> SignUp(CreateFSUserCommand command);
        public Task<FSUser> Login(LoginFSUserCommand command);
        public string GenerateToken(FSUser user);
    }
}