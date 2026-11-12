using BackEnd.Helpers;
using BackEnd.Data;
using BackEnd.DTOs.Category;
using BackEnd.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace BackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductCategoriesController : ControllerBase
    {
        private readonly IProductCategoryService _service;

        public ProductCategoriesController(IProductCategoryService service)
        {
            _service = service;
        }

        [HttpGet]
        [AllowAnonymous]
        [OutputCache(PolicyName = "PublicShort")]
        public async Task<IActionResult> GetActiveCategoryTree()
        {
            return Ok(await _service.GetTreeAsync(false));
        }

        [HttpGet("manage")]
        [Authorize(Policy = AppPermissions.CategoriesManage)]
        public async Task<IActionResult> GetFullCategoryTree()
        {
            return Ok(await _service.GetTreeAsync(true));
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = AppPermissions.CategoriesManage)]
        public async Task<IActionResult> GetProductCategoryById(int id)
        {
            var category = await _service.GetByIdAsync(id);
            if (category is null)
            {
                return NotFound();
            }

            return Ok(category);
        }

        [HttpGet("{id:int}/image")]
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategoryImage(int id, [FromQuery] string? size)
        {
            var image = await _service.GetCategoryImageAsync(id, ImageSize.IsThumbnail(size));
            if (image is null)
            {
                return NotFound();
            }

            return File(image.Value.Stream, image.Value.ContentType);
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [Authorize(Policy = AppPermissions.CategoriesManage)]
        public async Task<IActionResult> CreateProductCategory([FromForm] CreateProductCategoryDto dto)
        {
            var category = await _service.CreateAsync(dto);

            return CreatedAtAction(nameof(GetProductCategoryById), new { id = category.Id }, category);
        }

        [HttpPatch("{id:int}")]
        [Consumes("multipart/form-data")]
        [Authorize(Policy = AppPermissions.CategoriesManage)]
        public async Task<IActionResult> EditProductCategory(int id, [FromForm] PatchProductCategoryDto dto)
        {
            var category = await _service.PatchAsync(id, dto);
            if (category is null)
            {
                return NotFound();
            }

            return Ok(category);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = AppPermissions.CategoriesManage)]
        public async Task<IActionResult> DeleteProductCategory(int id)
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