using BackEnd.Helpers;
using BackEnd.Data;
using BackEnd.DTOs.ProductDTOs;
using BackEnd.IServices.IProductServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _service;
        private readonly IProductGalleryService _gallery;
        private readonly IProductAttributeService _attributes;

        public ProductsController(
            IProductService service,
            IProductGalleryService gallery,
            IProductAttributeService attributes)
        {
            _service = service;
            _gallery = gallery;
            _attributes = attributes;
        }

        [HttpGet]
        [Authorize(Policy = AppPermissions.ProductsView)]
        public async Task<IActionResult> GetAllProducts([FromQuery] ProductQueryDto query)
        {
            return Ok(await _service.GetAllAsync(query));
        }

        private void SetETag(byte[] rowVersion)
        {
            Response.Headers["ETag"] = RowVersionHelper.ToETag(RowVersionHelper.Encode(rowVersion));
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

            SetETag(product.RowVersion);

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

        [HttpGet("{id:int}/images")]
        [Authorize(Policy = AppPermissions.ProductsView)]
        public async Task<IActionResult> GetGallery(int id)
        {
            return Ok(await _gallery.GetImagesAsync(id, false));
        }

        [HttpGet("{id:int}/images/{imageId:int}/file")]
        [Authorize(Policy = AppPermissions.ProductsView)]
        public async Task<IActionResult> GetGalleryImage(int id, int imageId, [FromQuery] string? size)
        {
            var image = await _gallery.OpenAsync(id, imageId, ImageSize.IsThumbnail(size), false);
            if (image is null)
            {
                return NotFound();
            }

            return File(image.Value.Stream, image.Value.ContentType);
        }

        [HttpPost("{id:int}/images")]
        [Consumes("multipart/form-data")]
        [EnableRateLimiting("upload")]
        [Authorize(Policy = AppPermissions.ProductsManage)]
        public async Task<IActionResult> AddGalleryImage(int id, [FromForm] UploadProductImageDto dto)
        {
            var image = await _gallery.AddAsync(id, dto.Image, dto.AltText);
            if (image is null)
            {
                return NotFound();
            }

            return Ok(image);
        }

        [HttpPatch("{id:int}/images/{imageId:int}")]
        [Authorize(Policy = AppPermissions.ProductsManage)]
        public async Task<IActionResult> UpdateGalleryImage(int id, int imageId, [FromBody] UpdateProductImageDto dto)
        {
            var image = await _gallery.UpdateAsync(id, imageId, dto.AltText);
            if (image is null)
            {
                return NotFound();
            }

            return Ok(image);
        }

        [HttpPut("{id:int}/images/order")]
        [Authorize(Policy = AppPermissions.ProductsManage)]
        public async Task<IActionResult> ReorderGallery(int id, [FromBody] ReorderProductImagesDto dto)
        {
            var images = await _gallery.ReorderAsync(id, dto.ImageIds);
            if (images is null)
            {
                return NotFound();
            }

            return Ok(images);
        }

        [HttpDelete("{id:int}/images/{imageId:int}")]
        [Authorize(Policy = AppPermissions.ProductsManage)]
        public async Task<IActionResult> DeleteGalleryImage(int id, int imageId)
        {
            if (!await _gallery.DeleteAsync(id, imageId))
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpGet("{id:int}/attributes")]
        [Authorize(Policy = AppPermissions.ProductsView)]
        public async Task<IActionResult> GetAttributes(int id)
        {
            return Ok(await _attributes.GetValuesAsync(id));
        }

        [HttpPut("{id:int}/attributes")]
        [Authorize(Policy = AppPermissions.ProductsManage)]
        public async Task<IActionResult> SetAttributes(int id, [FromBody] SetProductAttributesDto dto)
        {
            var values = await _attributes.SetValuesAsync(id, dto.Items);
            if (values is null)
            {
                return NotFound();
            }

            return Ok(values);
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [Authorize(Policy = AppPermissions.ProductsManage)]
        public async Task<IActionResult> CreateProduct([FromForm] CreateProductDto dto)
        {
            var product = await _service.CreateAsync(dto);

            SetETag(product.RowVersion);

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

            SetETag(product.RowVersion);

            return Ok(product);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = AppPermissions.ProductsManage)]
        public async Task<IActionResult> DeleteProduct(int id, [FromHeader(Name = "If-Match")] string? rowVersion)
        {
            var deleted = await _service.DeleteAsync(id, rowVersion);
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
            var product = await _service.SetActiveAsync(id, dto.IsActive, dto.RowVersion);
            if (product is null)
            {
                return NotFound();
            }

            SetETag(product.RowVersion);

            return Ok(product);
        }
    }
}