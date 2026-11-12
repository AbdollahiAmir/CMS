using System.Security.Claims;
using BackEnd.Data;
using BackEnd.DTOs.Posts;
using BackEnd.DTOs.Revisions;
using BackEnd.Helpers;
using BackEnd.IServices;
using BackEnd.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PostsController : ControllerBase
    {
        private readonly IPostService _service;
        private readonly IPublicContentService _publicContent;
        private readonly IRevisionService _revisions;

        public PostsController(
            IPostService service,
            IPublicContentService publicContent,
            IRevisionService revisions)
        {
            _service = service;
            _publicContent = publicContent;
            _revisions = revisions;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        private void SetETag(string rowVersion)
        {
            Response.Headers["ETag"] = RowVersionHelper.ToETag(rowVersion);
        }

        [HttpGet]
        [Authorize(Policy = AppPermissions.PostsView)]
        public async Task<IActionResult> GetAll([FromQuery] PostQueryDto query)
        {
            return Ok(await _service.GetAllAsync(query));
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = AppPermissions.PostsView)]
        public async Task<IActionResult> GetById(int id)
        {
            var post = await _service.GetByIdAsync(id);

            if (post is null)
            {
                return NotFound();
            }

            SetETag(post.RowVersion);

            return Ok(post);
        }

        [HttpPost]
        [Authorize(Policy = AppPermissions.PostsManage)]
        public async Task<IActionResult> Create([FromBody] CreatePostDto dto)
        {
            var post = await _service.CreateAsync(dto, CurrentUserId);

            SetETag(post.RowVersion);

            return CreatedAtAction(nameof(GetById), new { id = post.Id }, post);
        }

        [HttpPatch("{id:int}")]
        [Authorize(Policy = AppPermissions.PostsManage)]
        public async Task<IActionResult> Patch(int id, [FromBody] PatchPostDto dto)
        {
            var post = await _service.PatchAsync(id, dto, CurrentUserId);

            if (post is null)
            {
                return NotFound();
            }

            SetETag(post.RowVersion);

            return Ok(post);
        }

        [HttpPatch("{id:int}/publication")]
        [Authorize(Policy = AppPermissions.PostsPublish)]
        public async Task<IActionResult> SetPublication(int id, [FromBody] SetPostPublicationDto dto)
        {
            var post = await _service.SetPublicationAsync(
                id,
                dto.IsPublished,
                CurrentUserId,
                dto.RowVersion);

            if (post is null)
            {
                return NotFound();
            }

            SetETag(post.RowVersion);

            return Ok(post);
        }

        [HttpPatch("{id:int}/schedule")]
        [Authorize(Policy = AppPermissions.PostsPublish)]
        public async Task<IActionResult> SetSchedule(int id, [FromBody] SchedulePostDto dto)
        {
            var post = await _service.SetScheduleAsync(
                id,
                dto.ScheduledAt,
                CurrentUserId,
                dto.RowVersion);

            if (post is null)
            {
                return NotFound();
            }

            SetETag(post.RowVersion);

            return Ok(post);
        }

        [HttpGet("{id:int}/revisions")]
        [Authorize(Policy = AppPermissions.PostsView)]
        public async Task<IActionResult> GetRevisions(int id)
        {
            return Ok(await _revisions.ListAsync(RevisionTypes.Post, id));
        }

        [HttpGet("{id:int}/revisions/{revisionId:int}")]
        [Authorize(Policy = AppPermissions.PostsView)]
        public async Task<IActionResult> GetRevision(int id, int revisionId)
        {
            var revision = await _revisions.GetAsync(RevisionTypes.Post, id, revisionId);

            if (revision is null)
            {
                return NotFound();
            }

            return Ok(revision);
        }

        [HttpPost("{id:int}/revisions/{revisionId:int}/restore")]
        [Authorize(Policy = AppPermissions.PostsManage)]
        public async Task<IActionResult> RestoreRevision(
            int id,
            int revisionId,
            [FromBody] RestoreRevisionDto dto)
        {
            var post = await _service.RestoreRevisionAsync(
                id,
                revisionId,
                CurrentUserId,
                dto.RowVersion);

            if (post is null)
            {
                return NotFound();
            }

            SetETag(post.RowVersion);

            return Ok(post);
        }

        [HttpGet("{id:int}/preview")]
        [Authorize(Policy = AppPermissions.PostsView)]
        public async Task<IActionResult> Preview(int id)
        {
            var post = await _publicContent.GetPostPreviewAsync(id);

            if (post is null)
            {
                return NotFound();
            }

            Response.Headers.CacheControl = "no-store";

            return Ok(post);
        }

        [HttpPost("{id:int}/featured-image")]
        [Consumes("multipart/form-data")]
        [Authorize(Policy = AppPermissions.PostsManage)]
        public async Task<IActionResult> UploadFeaturedImage(int id, [FromForm] IFormFile image, [FromHeader(Name = "If-Match")] string? rowVersion)
        {
            var saved = await _service.SaveFeaturedImageAsync(
                id,
                image,
                CurrentUserId,
                rowVersion);

            if (!saved)
            {
                return NotFound();
            }

            var post = await _service.GetByIdAsync(id);

            if (post is not null)
            {
                SetETag(post.RowVersion);
            }

            return Ok(new
            {
                message = "تصویر شاخص مقاله با موفقیت ذخیره شد",
                imageUrl = $"/api/Posts/{id}/featured-image",
                rowVersion = post?.RowVersion
            });
        }

        [HttpGet("{id:int}/featured-image")]
        [Authorize(Policy = AppPermissions.PostsView)]
        public async Task<IActionResult> GetFeaturedImage(int id, [FromQuery] string? size)
        {
            var image = await _service.GetFeaturedImageAsync(id, ImageSize.IsThumbnail(size));

            if (image is null)
            {
                return NotFound();
            }

            return File(image.Value.Stream, image.Value.ContentType);
        }

        [HttpDelete("{id:int}/featured-image")]
        [Authorize(Policy = AppPermissions.PostsManage)]
        public async Task<IActionResult> DeleteFeaturedImage(int id, [FromHeader(Name = "If-Match")] string? rowVersion)
        {
            var deleted = await _service.DeleteFeaturedImageAsync(
                id,
                CurrentUserId,
                rowVersion);

            if (!deleted)
            {
                return NotFound();
            }

            var post = await _service.GetByIdAsync(id);

            if (post is not null)
            {
                SetETag(post.RowVersion);
            }

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = AppPermissions.PostsManage)]
        public async Task<IActionResult> Delete(int id, [FromHeader(Name = "If-Match")] string? rowVersion)
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