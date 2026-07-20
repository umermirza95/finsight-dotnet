using System.Threading.Tasks;

namespace Finsight.Interfaces
{
    public interface IMessagingService
    {
        Task SendMessageAsync(string message);
    }
}
