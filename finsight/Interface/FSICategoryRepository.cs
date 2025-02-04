
using Finsight.Models;

namespace Finsight.Interface
{
    public interface FSICategoryRepository
    {
        IAsyncEnumerable<FSCategoryModel> GetAllAsync(string userId);
        // Category FindById(string id);
        // Category Add(AddCategoryCommand obj);
        // Category Update(Category obj);
        // void Delete(string id);
    }
}