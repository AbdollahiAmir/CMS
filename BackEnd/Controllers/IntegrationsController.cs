using System.Security.Claims;
using BackEnd.Data;
using BackEnd.DTOs.Integrations;
using BackEnd.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class IntegrationsController : ControllerBase
    {
        private readonly IIntegrationService _service;

        public IntegrationsController(IIntegrationService service)
        {
            _service = service;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        [HttpGet("email")]
        [Authorize(Policy = AppPermissions.MessagingManage)]
        public async Task<IActionResult> GetEmail()
        {
            return Ok(await _service.GetEmailAsync());
        }

        [HttpPut("email")]
        [Authorize(Policy = AppPermissions.MessagingManage)]
        public async Task<IActionResult> UpdateEmail([FromBody] UpdateEmailIntegrationDto dto)
        {
            return Ok(await _service.UpdateEmailAsync(dto, CurrentUserId));
        }

        [HttpPost("email/test")]
        [EnableRateLimiting("auth")]
        [Authorize(Policy = AppPermissions.MessagingManage)]
        public async Task<IActionResult> TestEmail([FromBody] TestEmailDto dto)
        {
            await _service.SendTestEmailAsync(dto.To);

            return Ok(new { message = "ایمیل آزمایشی ارسال شد" });
        }

        [HttpGet("sms")]
        [Authorize(Policy = AppPermissions.MessagingManage)]
        public async Task<IActionResult> GetSms()
        {
            return Ok(await _service.GetSmsAsync());
        }

        [HttpPut("sms")]
        [Authorize(Policy = AppPermissions.MessagingManage)]
        public async Task<IActionResult> UpdateSms([FromBody] UpdateSmsIntegrationDto dto)
        {
            return Ok(await _service.UpdateSmsAsync(dto, CurrentUserId));
        }

        [HttpPost("sms/test")]
        [EnableRateLimiting("auth")]
        [Authorize(Policy = AppPermissions.MessagingManage)]
        public async Task<IActionResult> TestSms([FromBody] TestSmsDto dto)
        {
            await _service.SendTestSmsAsync(dto.PhoneNumber);

            return Ok(new { message = "پیامک آزمایشی ارسال شد" });
        }

        [HttpGet("payment")]
        [Authorize(Policy = AppPermissions.PaymentManage)]
        public async Task<IActionResult> GetPayment()
        {
            return Ok(await _service.GetPaymentAsync());
        }

        [HttpPut("payment")]
        [Authorize(Policy = AppPermissions.PaymentManage)]
        public async Task<IActionResult> UpdatePayment([FromBody] UpdatePaymentIntegrationDto dto)
        {
            return Ok(await _service.UpdatePaymentAsync(dto, CurrentUserId));
        }

        [HttpGet("enamad")]
        [Authorize(Policy = AppPermissions.EnamadManage)]
        public async Task<IActionResult> GetEnamad()
        {
            return Ok(await _service.GetEnamadAsync());
        }

        [HttpPut("enamad")]
        [Authorize(Policy = AppPermissions.EnamadManage)]
        public async Task<IActionResult> UpdateEnamad([FromBody] UpdateEnamadIntegrationDto dto)
        {
            return Ok(await _service.UpdateEnamadAsync(dto, CurrentUserId));
        }
    }
}
