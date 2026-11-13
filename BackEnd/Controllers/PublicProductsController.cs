using BackEnd.Helpers;
using BackEnd.DTOs.ProductDTOs;
using BackEnd.IServices.IProductServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

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
        [OutputCache(PolicyName = "PublicShort")]
        public async Task<IActionResult> GetPublicProducts([FromQuery] ProductQueryDto query)
        {
            return Ok(await _service.GetProductsAsync(query));
        }

        [HttpPost("cart-lookup")]
        [EnableRateLimiting("lookup")]
        public async Task<IActionResult> LookupForCart(CartLookupRequestDto dto)
        {
            return Ok(await _service.LookupForCartAsync(dto.ProductIds));
        }

        [HttpGet("products/{slug}")]
        [OutputCache(PolicyName = "PublicShort")]
        public async Task<IActionResult> GetPublicProductBySlug(string slug)
        {
            var product = await _service.GetBySlugAsync(slug);
            if (product is null)
            {
                return NotFound();
            }

            return Ok(product);
        }

        [HttpGet("attributes")]
        [OutputCache(PolicyName = "PublicShort")]
        public async Task<IActionResult> GetFilterAttributes()
        {
            return Ok(await _service.GetFilterAttributesAsync());
        }

        [HttpGet("products/{id:int}/images/{imageId:int}")]
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> GetPublicGalleryImage(int id, int imageId, [FromQuery] string? size)
        {
            var image = await _service.GetGalleryImageAsync(id, imageId, ImageSize.IsThumbnail(size));
            if (image is null)
            {
                return NotFound();
            }

            return File(image.Value.Stream, image.Value.ContentType);
        }

        [HttpGet("products/{id:int}/image")]
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> GetPublicProductImage(int id, [FromQuery] string? size)
        {
            var image = await _service.GetProductImageAsync(id, ImageSize.IsThumbnail(size));
            if (image is null)
            {
                return NotFound();
            }

            return File(image.Value.Stream, image.Value.ContentType);
        }
    }
}