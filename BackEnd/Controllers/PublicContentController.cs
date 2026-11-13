using BackEnd.Helpers;
using BackEnd.DTOs.PublicContent;
using BackEnd.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace BackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class PublicContentController : ControllerBase
    {
        private readonly IPublicContentService _service;

        public PublicContentController(IPublicContentService service)
        {
            _service = service;
        }

        [HttpGet("pages")]
        [OutputCache(PolicyName = "PublicShort")]
        public async Task<IActionResult> GetPages()
        {
            return Ok(await _service.GetPagesAsync());
        }

        [HttpGet("pages/{slug}")]
        [OutputCache(PolicyName = "PublicShort")]
        public async Task<IActionResult> GetPage(string slug)
        {
            var page = await _service.GetPageBySlugAsync(slug);

            if (page is null)
            {
                return NotFound();
            }

            return Ok(page);
        }

        [HttpGet("posts")]
        [OutputCache(PolicyName = "PublicShort")]
        public async Task<IActionResult> GetPosts(
            [FromQuery] PublicPostQueryDto query)
        {
            return Ok(await _service.GetPostsAsync(query));
        }

        [HttpGet("posts/{slug}")]
        [OutputCache(PolicyName = "PublicShort")]
        public async Task<IActionResult> GetPost(string slug)
        {
            var post = await _service.GetPostBySlugAsync(slug);

            if (post is null)
            {
                return NotFound();
            }

            return Ok(post);
        }

        [HttpGet("posts/{slug}/related")]
        [OutputCache(PolicyName = "PublicShort")]
        public async Task<IActionResult> GetRelatedPosts(string slug, [FromQuery] int count = 4)
        {
            var posts = await _service.GetRelatedPostsAsync(slug, count);

            if (posts is null)
            {
                return NotFound();
            }

            return Ok(posts);
        }

        [HttpGet("search")]
        [EnableRateLimiting("lookup")]
        [OutputCache(PolicyName = "PublicShort")]
        public async Task<IActionResult> Search([FromQuery] PublicSearchQueryDto query)
        {
            return Ok(await _service.SearchAsync(query.Q ?? string.Empty, query.Limit));
        }

        [HttpGet("categories")]
        [OutputCache(PolicyName = "PublicShort")]
        public async Task<IActionResult> GetCategories()
        {
            return Ok(await _service.GetCategoriesAsync());
        }

        [HttpGet("tags")]
        [OutputCache(PolicyName = "PublicShort")]
        public async Task<IActionResult> GetTags()
        {
            return Ok(await _service.GetTagsAsync());
        }

        [HttpGet("posts/{slug}/featured-image")]
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> GetPostFeaturedImage(string slug, [FromQuery] string? size)
        {
            var image = await _service.GetPostFeaturedImageAsync(slug, ImageSize.IsThumbnail(size));

            if (image is null)
            {
                return NotFound();
            }

            return File(image.Value.Stream, image.Value.ContentType);
        }
    }
}