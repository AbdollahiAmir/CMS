using BackEnd.Helpers;
using BackEnd.Data;
using BackEnd.DTOs.ProductDTOs;
using BackEnd.IServices.IProductServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _service;

        public ProductsController(IProductService service)
        {
            _service = service;
        }

        [HttpGet]
        [Authorize(Policy = AppPermissions.ProductsView)]
        public async Task<IActionResult> GetAllProducts([FromQuery] ProductQueryDto query)
        {
            return Ok(await _service.GetAllAsync(query));
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = AppPermissions.ProductsView)]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _service.GetByIdAsync(id);
            if (product is null)
            {
                return NotFound();
            }

            return Ok(product);
        }

        [HttpGet("{id:int}/image")]
        [Authorize(Policy = AppPermissions.ProductsView)]
        public async Task<IActionResult> GetProductImage(int id, [FromQuery] string? size)
        {
            var image = await _service.GetProductImageAsync(id, ImageSize.IsThumbnail(size));
            if (image is null)
            {
                return NotFound();
            }

            return File(image.Value.Stream, image.Value.ContentType);
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [Authorize(Policy = AppPermissions.ProductsManage)]
        public async Task<IActionResult> CreateProduct([FromForm] CreateProductDto dto)
        {
            var product = await _service.CreateAsync(dto);

            return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
        }

        [HttpPatch("{id:int}")]
        [Consumes("multipart/form-data")]
        [Authorize(Policy = AppPermissions.ProductsManage)]
        public async Task<IActionResult> EditProduct(int id, [FromForm] PatchProductDto dto)
        {
            var product = await _service.PatchAsync(id, dto);
            if (product is null)
            {
                return NotFound();
            }

            return Ok(product);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = AppPermissions.ProductsManage)]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpPatch("{id:int}/active")]
        [Authorize(Policy = AppPermissions.ProductsToggleActive)]
        public async Task<IActionResult> SetActive(int id, SetProductActiveDto dto)
        {
            var product = await _service.SetActiveAsync(id, dto.IsActive);
            if (product is null)
            {
                return NotFound();
            }

            return Ok(product);
        }
    }
}