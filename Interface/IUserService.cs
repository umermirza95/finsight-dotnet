using Finsight.Commands;
using Finsight.Models;

namespace Finsight.Interfaces
{
    public interface IFSUserService
    {
        public Task<FSUser> SignUp(CreateFSUserCommand command);
    }
}