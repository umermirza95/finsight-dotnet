
using System.Threading.Tasks;
using Finsight.Models;
using Finsight.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Finsight.Interface;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Finsight.Controller
{
    [ApiController]
    [Authorize]
    [Route("api/category")]
    public class FSCategoryController : ControllerBase
    {
        FSICategoryRepository categoryRepository;
        public FSCategoryController(FSICategoryRepository categoryRepository)
        {
            this.categoryRepository = categoryRepository;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IAsyncEnumerable<FSCategoryModel> GetCategoriesAsync()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            return categoryRepository.FetchAsync(userId);
        }
    }
}