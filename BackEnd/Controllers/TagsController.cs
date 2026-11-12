using BackEnd.Data;
using BackEnd.DTOs.Tags;
using BackEnd.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TagsController : ControllerBase
    {
        private readonly ITagService _service;

        public TagsController(ITagService service)
        {
            _service = service;
        }

        [HttpGet]
        [Authorize(Policy = AppPermissions.PostsView)]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = AppPermissions.PostsView)]
        public async Task<IActionResult> GetById(int id)
        {
            var tag = await _service.GetByIdAsync(id);

            if (tag is null)
            {
                return NotFound();
            }

            return Ok(tag);
        }

        [HttpPost]
        [Authorize(Policy = AppPermissions.TagsManage)]
        public async Task<IActionResult> Create([FromBody] CreateTagDto dto)
        {
            var tag = await _service.CreateAsync(dto);

            return CreatedAtAction(nameof(GetById), new { id = tag.Id }, tag);
        }

        [HttpPatch("{id:int}")]
        [Authorize(Policy = AppPermissions.TagsManage)]
        public async Task<IActionResult> Patch(int id, [FromBody] PatchTagDto dto)
        {
            var tag = await _service.PatchAsync(id, dto);

            if (tag is null)
            {
                return NotFound();
            }

            return Ok(tag);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = AppPermissions.TagsManage)]
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