using System.Security.Claims;
using BackEnd.Data;
using BackEnd.DTOs.Pages;
using BackEnd.IServices;
using BackEnd.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ContentPagesController : ControllerBase
    {
        private readonly IContentPageService _service;

        public ContentPagesController(IContentPageService service)
        {
            _service = service;
        }

        private string? CurrentUserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        private void SetETag(string rowVersion)
        {
            Response.Headers["ETag"] = RowVersionHelper.ToETag(rowVersion);
        }

        [HttpGet]
        [Authorize(Policy = AppPermissions.PagesView)]
        public async Task<IActionResult> GetPages([FromQuery] PageQueryDto query)
        {
            return Ok(await _service.GetAllAsync(query));
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = AppPermissions.PagesView)]
        public async Task<IActionResult> GetPage(int id)
        {
            var page = await _service.GetByIdAsync(id);

            if (page is null)
            {
                return NotFound();
            }

            SetETag(page.RowVersion);

            return Ok(page);
        }

        [HttpPost]
        [Authorize(Policy = AppPermissions.PagesManage)]
        public async Task<IActionResult> CreatePage([FromBody] CreateContentPageDto dto)
        {
            var page = await _service.CreateAsync(dto, CurrentUserId);

            SetETag(page.RowVersion);

            return CreatedAtAction(nameof(GetPage), new { id = page.Id }, page);
        }

        [HttpPatch("{id:int}")]
        [Authorize(Policy = AppPermissions.PagesManage)]
        public async Task<IActionResult> EditPage(
            int id,
            [FromBody] PatchContentPageDto dto)
        {
            var page = await _service.PatchAsync(id, dto, CurrentUserId);

            if (page is null)
            {
                return NotFound();
            }

            SetETag(page.RowVersion);

            return Ok(page);
        }

        [HttpPatch("{id:int}/publication")]
        [Authorize(Policy = AppPermissions.PagesPublish)]
        public async Task<IActionResult> SetPublication(
            int id,
            [FromBody] SetPagePublicationDto dto)
        {
            var page = await _service.SetPublicationAsync(
                id,
                dto.IsPublished,
                dto.RowVersion);

            if (page is null)
            {
                return NotFound();
            }

            SetETag(page.RowVersion);

            return Ok(page);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = AppPermissions.PagesManage)]
        public async Task<IActionResult> DeletePage(
            int id,
            [FromHeader(Name = "If-Match")] string? rowVersion)
        {
            var deleted = await _service.DeleteAsync(id, rowVersion);

            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}