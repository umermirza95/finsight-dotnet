
using Finsight.Interfaces;
using Finsight.Models;
using Microsoft.EntityFrameworkCore;

namespace Finsight.Services
{
    public class FSCategoryService(IDbContextFactory<AppDbContext> dbFactory) : ICategoryService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

        public async Task<List<FSCategory>> GetCategoriesAsync(string userId)
        {
            using var _context = await _dbFactory.CreateDbContextAsync();
            return await _context.Categories
                        .Include(c => c.SubCategories)
                        .ToListAsync();
        }
    }
}