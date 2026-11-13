using BackEnd.Data;
using BackEnd.DTOs.ProductDTOs;
using BackEnd.IServices.IProductServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductAttributesController : ControllerBase
    {
        private readonly IProductAttributeService _service;

        public ProductAttributesController(IProductAttributeService service)
        {
            _service = service;
        }

        [HttpGet]
        [Authorize(Policy = AppPermissions.ProductsView)]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }

        [HttpPost]
        [Authorize(Policy = AppPermissions.ProductsManage)]
        public async Task<IActionResult> Create([FromBody] CreateProductAttributeDto dto)
        {
            return Ok(await _service.CreateAsync(dto));
        }

        [HttpPatch("{id:int}")]
        [Authorize(Policy = AppPermissions.ProductsManage)]
        public async Task<IActionResult> Patch(int id, [FromBody] PatchProductAttributeDto dto)
        {
            var attribute = await _service.PatchAsync(id, dto);
            if (attribute is null)
            {
                return NotFound();
            }

            return Ok(attribute);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = AppPermissions.ProductsManage)]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await _service.DeleteAsync(id))
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
