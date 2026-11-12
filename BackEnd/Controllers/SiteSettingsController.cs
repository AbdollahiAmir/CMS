using System.Security.Claims;
using BackEnd.Data;
using BackEnd.DTOs.Settings;
using BackEnd.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SiteSettingsController : ControllerBase
    {
        private readonly ISiteSettingsService _service;

        public SiteSettingsController(ISiteSettingsService service)
        {
            _service = service;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        [HttpGet]
        [Authorize(Policy = AppPermissions.SettingsManage)]
        public async Task<IActionResult> Get()
        {
            return Ok(await _service.GetAsync());
        }

        [HttpPut]
        [Authorize(Policy = AppPermissions.SettingsManage)]
        public async Task<IActionResult> Update([FromBody] UpdateSiteSettingsDto dto)
        {
            return Ok(await _service.UpdateAsync(dto, CurrentUserId));
        }
    }
}
