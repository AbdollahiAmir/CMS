using BackEnd.Data;
using BackEnd.DTOs.Menus;
using BackEnd.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MenusController : ControllerBase
    {
        private readonly IMenuService _service;

        public MenusController(IMenuService service)
        {
            _service = service;
        }

        [HttpGet]
        [Authorize(Policy = AppPermissions.MenusManage)]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = AppPermissions.MenusManage)]
        public async Task<IActionResult> GetById(int id)
        {
            var menu = await _service.GetByIdAsync(id);

            if (menu is null)
            {
                return NotFound();
            }

            return Ok(menu);
        }

        [HttpPost]
        [Authorize(Policy = AppPermissions.MenusManage)]
        public async Task<IActionResult> Create([FromBody] CreateMenuDto dto)
        {
            var menu = await _service.CreateAsync(dto);

            return CreatedAtAction(nameof(GetById), new { id = menu.Id }, menu);
        }

        [HttpPatch("{id:int}")]
        [Authorize(Policy = AppPermissions.MenusManage)]
        public async Task<IActionResult> Patch(int id, [FromBody] PatchMenuDto dto)
        {
            var menu = await _service.PatchAsync(id, dto);

            if (menu is null)
            {
                return NotFound();
            }

            return Ok(menu);
        }

        [HttpPut("{id:int}/items")]
        [Authorize(Policy = AppPermissions.MenusManage)]
        public async Task<IActionResult> ReplaceItems(int id, [FromBody] SaveMenuItemsDto dto)
        {
            var menu = await _service.ReplaceItemsAsync(id, dto);

            if (menu is null)
            {
                return NotFound();
            }

            return Ok(menu);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = AppPermissions.MenusManage)]
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
