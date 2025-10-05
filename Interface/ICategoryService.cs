
using Finsight.Models;

namespace Finsight.Interfaces
{
    public interface ICategoryService
    {
        public Task<List<FSCategory>> GetCategoriesAsync(Guid userId);
    }    
}