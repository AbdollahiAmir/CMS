using BackEnd.DTOs.ProductDTOs;
using BackEnd.IServices.IProductServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class PublicProductsController : ControllerBase
    {
        private readonly IPublicProductService _service;

        public PublicProductsController(IPublicProductService service)
        {
            _service = service;
        }

        [HttpGet("products")]
        public async Task<IActionResult> GetPublicProducts([FromQuery] ProductQueryDto query)
        {
            return Ok(await _service.GetProductsAsync(query));
        }

        [HttpGet("products/{slug}")]
        public async Task<IActionResult> GetPublicProductBySlug(string slug)
        {
            var product = await _service.GetBySlugAsync(slug);
            if (product is null)
            {
                return NotFound();
            }

            return Ok(product);
        }

        [HttpGet("products/{id:int}/image")]
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> GetPublicProductImage(int id)
        {
            var image = await _service.GetProductImageAsync(id);
            if (image is null)
            {
                return NotFound();
            }

            return File(image.Value.Stream, image.Value.ContentType);
        }
    }
}