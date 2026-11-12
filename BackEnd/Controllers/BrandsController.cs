using BackEnd.Helpers;
using BackEnd.Data;
using BackEnd.DTOs.Brands;
using BackEnd.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace BackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BrandsController : ControllerBase
    {
        private readonly IBrandService _service;

        public BrandsController(IBrandService service)
        {
            _service = service;
        }

        [HttpGet]
        [AllowAnonymous]
        [OutputCache(PolicyName = "PublicShort")]
        public async Task<IActionResult> GetActiveBrands()
        {
            return Ok(await _service.GetAllAsync(false));
        }

        [HttpGet("manage")]
        [Authorize(Policy = AppPermissions.BrandsManage)]
        public async Task<IActionResult> GetAllBrands()
        {
            return Ok(await _service.GetAllAsync(true));
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = AppPermissions.BrandsManage)]
        public async Task<IActionResult> GetBrandById(int id)
        {
            var brand = await _service.GetByIdAsync(id);
            if (brand is null)
            {
                return NotFound();
            }

            return Ok(brand);
        }

        [HttpGet("{id:int}/logo")]
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
        [AllowAnonymous]
        public async Task<IActionResult> GetBrandLogo(int id, [FromQuery] string? size)
        {
            var logo = await _service.GetBrandLogoAsync(id, true, ImageSize.IsThumbnail(size));
            if (logo is null)
            {
                return NotFound();
            }

            return File(logo.Value.Stream, logo.Value.ContentType);
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [Authorize(Policy = AppPermissions.BrandsManage)]
        public async Task<IActionResult> CreateBrand([FromForm] CreateBrandDto dto)
        {
            var brand = await _service.CreateAsync(dto);

            return CreatedAtAction(nameof(GetBrandById), new { id = brand.Id }, brand);
        }

        [HttpPatch("{id:int}")]
        [Consumes("multipart/form-data")]
        [Authorize(Policy = AppPermissions.BrandsManage)]
        public async Task<IActionResult> EditBrand(int id, [FromForm] PatchBrandDto dto)
        {
            var brand = await _service.PatchAsync(id, dto);
            if (brand is null)
            {
                return NotFound();
            }

            return Ok(brand);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = AppPermissions.BrandsManage)]
        public async Task<IActionResult> DeleteBrand(int id)
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