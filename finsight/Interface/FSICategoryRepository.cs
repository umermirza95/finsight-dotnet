
using Finsight.Models;

namespace Finsight.Interface
{
    public interface FSICategoryRepository
    {
        IAsyncEnumerable<FSCategoryModel> FetchAsync(string userId);
    }
}