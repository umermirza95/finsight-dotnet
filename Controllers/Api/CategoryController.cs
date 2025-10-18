

using System.Security.Claims;
using Finsight.DTOs;
using Finsight.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finsight.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "JwtBearer")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;


        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetCategories()
        {
            var userIdString = "b2819fa8-5207-4dff-ab65-7ac14a42663b"; // User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var categories = await _categoryService.GetCategoriesAsync(userIdString);
            var categoryDtos = categories.Select(c => new FSCategoryDTO
            {
                Id = c.Id,
                Name = c.Name,
                Type = c.Type,
                SubCategories = [.. c.SubCategories.Select(s => new FSSubCategoryDTO
                {
                    Id = s.Id,
                    Name = s.Name
                })]
            }).ToList();
            return Ok(new
            {
                data = new { categories = categoryDtos}
            });
           
        }
    }
}