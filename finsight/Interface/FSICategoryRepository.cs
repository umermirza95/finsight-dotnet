
using Finsight.Models;

namespace Finsight.Interface
{
    public interface FSICategoryRepository
    {
        IAsyncEnumerable<FSCategoryModel> FetchAsync(string userId);
        Task<FSCategoryModel> GetByIdAsync(string userId, string categoryId);
    }
}