
using Finsight.Interfaces;
using Finsight.Models;
using Microsoft.EntityFrameworkCore;

namespace Finsight.Services
{
    public class FSCategoryService : ICategoryService
    {
        private readonly AppDbContext _context;

        public FSCategoryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<FSCategory>> GetCategoriesAsync(Guid userId)
        {
            return await _context.Categories
                        .Where(c => c.FSUserId == userId)
                        .Include(c => c.SubCategories)
                        .ToListAsync();
        }
    }
}