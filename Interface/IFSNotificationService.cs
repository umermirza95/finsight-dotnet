using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Finsight.Models;

namespace Finsight.Interfaces
{
    public interface IFSNotificationService
    {
        Task<FSNotification?> GetByIdAsync(Guid id);
        Task<IEnumerable<FSNotification>> GetAllForUserAsync(string userId);
        Task CreateTradeNotificationAsync(FSTrade trade);
        Task MarkAsReadAsync(Guid id);
        Task DeleteAsync(Guid id);
    }
}
