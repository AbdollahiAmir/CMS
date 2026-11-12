using System.Security.Claims;
using BackEnd.Data;
using BackEnd.DTOs.Media;
using BackEnd.Helpers;
using BackEnd.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MediaController : ControllerBase
    {
        private readonly IMediaService _service;

        public MediaController(IMediaService service)
        {
            _service = service;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        [HttpGet]
        [Authorize(Policy = AppPermissions.MediaView)]
        public async Task<IActionResult> GetAll([FromQuery] MediaQueryDto query)
        {
            return Ok(await _service.GetAllAsync(query));
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = AppPermissions.MediaView)]
        public async Task<IActionResult> GetById(int id)
        {
            var media = await _service.GetByIdAsync(id);

            if (media is null)
            {
                return NotFound();
            }

            return Ok(media);
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [EnableRateLimiting("upload")]
        [Authorize(Policy = AppPermissions.MediaManage)]
        public async Task<IActionResult> Upload([FromForm] UploadMediaDto dto)
        {
            var media = await _service.UploadAsync(dto.Image, dto.AltText, CurrentUserId);

            return CreatedAtAction(nameof(GetById), new { id = media.Id }, media);
        }

        [HttpPatch("{id:int}")]
        [Authorize(Policy = AppPermissions.MediaManage)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateMediaDto dto)
        {
            var media = await _service.UpdateAsync(id, dto);

            if (media is null)
            {
                return NotFound();
            }

            return Ok(media);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = AppPermissions.MediaManage)]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);

            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpGet("files/{fileName}")]
        [AllowAnonymous]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> GetFile(string fileName, [FromQuery] string? size)
        {
            var image = await _service.OpenFileAsync(fileName, ImageSize.IsThumbnail(size));

            if (image is null)
            {
                return NotFound();
            }

            return File(image.Value.Stream, image.Value.ContentType);
        }
    }
}
