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
        private readonly IPublicContentService _publicContent;

        public ContentPagesController(IContentPageService service, IPublicContentService publicContent)
        {
            _service = service;
            _publicContent = publicContent;
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
                CurrentUserId,
                dto.RowVersion);

            if (page is null)
            {
                return NotFound();
            }

            SetETag(page.RowVersion);

            return Ok(page);
        }

        [HttpPatch("{id:int}/schedule")]
        [Authorize(Policy = AppPermissions.PagesPublish)]
        public async Task<IActionResult> SetSchedule(
            int id,
            [FromBody] SchedulePageDto dto)
        {
            var page = await _service.SetScheduleAsync(
                id,
                dto.ScheduledAt,
                CurrentUserId,
                dto.RowVersion);

            if (page is null)
            {
                return NotFound();
            }

            SetETag(page.RowVersion);

            return Ok(page);
        }

        [HttpGet("{id:int}/preview")]
        [Authorize(Policy = AppPermissions.PagesView)]
        public async Task<IActionResult> Preview(int id)
        {
            var page = await _publicContent.GetPagePreviewAsync(id);

            if (page is null)
            {
                return NotFound();
            }

            Response.Headers.CacheControl = "no-store";

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