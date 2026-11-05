using BackEnd.Authorization;
using BackEnd.Data;
using BackEnd.DTOs.Roles;
using BackEnd.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = AppPermissions.RolesManage)]
    public class RolesController : ControllerBase
    {
        private readonly IRolesService _service;

        public RolesController(IRolesService service)
        {
            _service = service;
        }

        private IActionResult ToErrorResponse(RoleResult result)
        {
            if (result.NotFound)
            {
                return NotFound(new { errors = result.Errors });
            }

            if (result.Forbidden)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { errors = result.Errors });
            }

            return BadRequest(new { errors = result.Errors });
        }

        [HttpGet("permissions")]
        public IActionResult GetPermissions()
        {
            return Ok(_service.GetPermissions());
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRoles()
        {
            return Ok(await _service.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoleById(string id)
        {
            var role = await _service.GetByIdAsync(id);
            if (role is null)
            {
                return NotFound(new { message = "نقش یافت نشد" });
            }

            return Ok(role);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole(SaveRoleDto dto)
        {
            var result = await _service.CreateAsync(dto, Actor.From(User));
            if (!result.IsSuccess)
            {
                return ToErrorResponse(result);
            }

            return CreatedAtAction(nameof(GetRoleById), new { id = result.Role!.Id }, result.Role);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRole(string id, SaveRoleDto dto)
        {
            var result = await _service.UpdateAsync(id, dto, Actor.From(User));
            if (!result.IsSuccess)
            {
                return ToErrorResponse(result);
            }

            return Ok(result.Role);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(string id)
        {
            var result = await _service.DeleteAsync(id, Actor.From(User));
            if (!result.IsSuccess)
            {
                return ToErrorResponse(result);
            }

            return NoContent();
        }
    }
}