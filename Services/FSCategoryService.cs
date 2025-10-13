
using Finsight.Interfaces;
using Finsight.Models;
using Microsoft.EntityFrameworkCore;

namespace Finsight.Services
{
    public class FSCategoryService(AppDbContext context) : ICategoryService
    {
        private readonly AppDbContext _context = context;

        public async Task<List<FSCategory>> GetCategoriesAsync(string userId)
        {
            return await _context.Categories
                        .Where(c => c.FSUserId == userId)
                        .Include(c => c.SubCategories)
                        .ToListAsync();
        }
    }
}