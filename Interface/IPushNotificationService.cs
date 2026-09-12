using System.Threading.Tasks;

namespace Finsight.Interfaces
{
    public interface IPushNotificationService
    {
        Task SendNotificationAsync(string userId, string payload);
    }
}
