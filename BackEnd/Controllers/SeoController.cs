using BackEnd.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace BackEnd.Controllers
{
    [ApiController]
    [AllowAnonymous]
    public class SeoController : ControllerBase
    {
        private const string XmlContentType = "application/xml; charset=utf-8";

        private readonly ISeoService _service;

        public SeoController(ISeoService service)
        {
            _service = service;
        }

        [HttpGet("/sitemap.xml")]
        [OutputCache(PolicyName = "PublicShort")]
        public async Task<IActionResult> Sitemap()
        {
            var xml = await _service.BuildSitemapAsync();

            if (xml is null)
            {
                return NotFound();
            }

            return Content(xml, XmlContentType);
        }

        [HttpGet("/rss.xml")]
        [OutputCache(PolicyName = "PublicShort")]
        public async Task<IActionResult> Rss()
        {
            var xml = await _service.BuildRssAsync();

            if (xml is null)
            {
                return NotFound();
            }

            return Content(xml, XmlContentType);
        }
    }
}
