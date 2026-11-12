using BackEnd.Data;
using BackEnd.DTOs.PostCategories;
using BackEnd.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PostCategoriesController : ControllerBase
    {
        private readonly IPostCategoryService _service;

        public PostCategoriesController(IPostCategoryService service)
        {
            _service = service;
        }

        [HttpGet]
        [Authorize(Policy = AppPermissions.PostsView)]
        public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
        {
            return Ok(await _service.GetAllAsync(includeInactive));
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = AppPermissions.PostsView)]
        public async Task<IActionResult> GetById(int id)
        {
            var category = await _service.GetByIdAsync(id);
            if (category is null)
            {
                return NotFound();
            }

            return Ok(category);
        }

        [HttpPost]
        [Authorize(Policy = AppPermissions.PostCategoriesManage)]
        public async Task<IActionResult> Create([FromBody] CreatePostCategoryDto dto)
        {
            var category = await _service.CreateAsync(dto);

            return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
        }

        [HttpPatch("{id:int}")]
        [Authorize(Policy = AppPermissions.PostCategoriesManage)]
        public async Task<IActionResult> Patch(int id, [FromBody] PatchPostCategoryDto dto)
        {
            var category = await _service.PatchAsync(id, dto);
            if (category is null)
            {
                return NotFound();
            }

            return Ok(category);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = AppPermissions.PostCategoriesManage)]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}