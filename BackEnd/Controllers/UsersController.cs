using BackEnd.Authorization;
using BackEnd.Data;
using BackEnd.DTOs.Users;
using BackEnd.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = AppPermissions.UsersManage)]
    public class UsersController : ControllerBase
    {
        private readonly IUsersService _service;

        public UsersController(IUsersService service)
        {
            _service = service;
        }

        private IActionResult ToErrorResponse(UserResult result)
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

        [HttpGet]
        public async Task<IActionResult> GetAllUsers([FromQuery] UserQueryDto query)
        {
            var users = await _service.GetAllAsync(query);
            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var user = await _service.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "کاربر یافت نشد." });
            }

            return Ok(user);
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> EditUser(string id, EditUserDto dto)
        {
            var result = await _service.EditAsync(id, dto, Actor.From(User));
            if (!result.Succeeded)
            {
                return ToErrorResponse(result);
            }

            return Ok(new { message = "ویرایش کاربر موفقیت آمیز بود" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var result = await _service.DeleteAsync(id, Actor.From(User));
            if (!result.Succeeded)
            {
                return ToErrorResponse(result);
            }

            return Ok(new { message = "کاربر با موفقیت حذف شد." });
        }
    }
}