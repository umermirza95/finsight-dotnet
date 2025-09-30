using Finsight.Interfaces;
using Finsight.Models;

namespace Finsight.Services
{
    public class FSUserService : IFSUserService
    {
        public async Task<FSUser> SignUp()
        {
            return new FSUser();
        }
    }    
}