using System.Security.Claims;
using BackEnd.Data;
using BackEnd.DTOs.Comments;
using BackEnd.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PostCommentsController : ControllerBase
    {
        private readonly ICommentService _service;

        public PostCommentsController(ICommentService service)
        {
            _service = service;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        [HttpGet]
        [Authorize(Policy = AppPermissions.CommentsModerate)]
        public async Task<IActionResult> GetAll([FromQuery] AdminCommentQueryDto query)
        {
            return Ok(await _service.GetAllAsync(query));
        }

        [HttpGet("pending-count")]
        [Authorize(Policy = AppPermissions.CommentsModerate)]
        public async Task<IActionResult> GetPendingCount()
        {
            return Ok(new { count = await _service.GetPendingCountAsync() });
        }

        [HttpPatch("{id:int}/status")]
        [Authorize(Policy = AppPermissions.CommentsModerate)]
        public async Task<IActionResult> SetStatus(int id, [FromBody] SetCommentStatusDto dto)
        {
            var comment = await _service.SetStatusAsync(id, dto.Status, CurrentUserId);

            if (comment is null)
            {
                return NotFound();
            }

            return Ok(comment);
        }

        [HttpPost("bulk-status")]
        [Authorize(Policy = AppPermissions.CommentsModerate)]
        public async Task<IActionResult> SetStatusBulk([FromBody] BulkCommentStatusDto dto)
        {
            var updated = await _service.SetStatusBulkAsync(dto.Ids, dto.Status, CurrentUserId);

            return Ok(new { updated });
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = AppPermissions.CommentsModerate)]
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
