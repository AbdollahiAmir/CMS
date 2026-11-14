using BackEnd.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace BackEnd.Controllers
{
    [Route("api/PublicContent")]
    [ApiController]
    [AllowAnonymous]
    public class PublicSiteController : ControllerBase
    {
        private readonly IMenuService _menus;
        private readonly ISiteSettingsService _settings;
        private readonly IIntegrationService _integrations;

        public PublicSiteController(IMenuService menus, ISiteSettingsService settings, IIntegrationService integrations)
        {
            _menus = menus;
            _settings = settings;
            _integrations = integrations;
        }

        [HttpGet("menus/{key}")]
        [OutputCache(PolicyName = "PublicShort")]
        public async Task<IActionResult> GetMenu(string key)
        {
            var menu = await _menus.GetByKeyAsync(key);

            if (menu is null)
            {
                return NotFound();
            }

            return Ok(menu);
        }

        [HttpGet("trust-badges")]
        [OutputCache(PolicyName = "PublicShort")]
        public async Task<IActionResult> GetTrustBadges()
        {
            return Ok(await _integrations.GetPublicTrustBadgesAsync());
        }

        [HttpGet("site-settings")]
        [OutputCache(PolicyName = "PublicShort")]
        public async Task<IActionResult> GetSettings()
        {
            return Ok(await _settings.GetAsync());
        }
    }
}
