using System.Security.Claims;
using BackEnd.DTOs.Account;
using BackEnd.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _service;

        public AccountController(IAccountService service)
        {
            _service = service;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            if (CurrentUserId is null)
            {
                return Unauthorized();
            }

            var profile = await _service.GetProfileAsync(CurrentUserId);

            return profile is null ? NotFound() : Ok(profile);
        }

        [HttpPatch("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            if (CurrentUserId is null)
            {
                return Unauthorized();
            }

            var profile = await _service.UpdateProfileAsync(CurrentUserId, dto);

            return profile is null ? NotFound() : Ok(profile);
        }

        [HttpPut("email")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailDto dto)
        {
            if (CurrentUserId is null)
            {
                return Unauthorized();
            }

            var result = await _service.RequestEmailChangeAsync(CurrentUserId, dto.Email);

            if (!result.IsSuccess)
            {
                return BadRequest(new { errors = result.Errors });
            }

            return Ok(new { message = "لینک تایید به ایمیل شما ارسال شد" });
        }

        [HttpPost("email/resend-confirmation")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ResendEmailConfirmation()
        {
            if (CurrentUserId is null)
            {
                return Unauthorized();
            }

            var result = await _service.ResendEmailConfirmationAsync(CurrentUserId);

            if (!result.IsSuccess)
            {
                return BadRequest(new { errors = result.Errors });
            }

            return Ok(new { message = "لینک تایید دوباره ارسال شد" });
        }

        [HttpPut("phone")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> RequestPhoneCode([FromBody] RequestPhoneCodeDto dto)
        {
            if (CurrentUserId is null)
            {
                return Unauthorized();
            }

            var result = await _service.RequestPhoneCodeAsync(CurrentUserId, dto.PhoneNumber);

            if (!result.IsSuccess)
            {
                return BadRequest(new { errors = result.Errors });
            }

            return Ok(new { message = "کد تایید ارسال شد" });
        }

        [HttpPost("phone/verify")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> VerifyPhone([FromBody] VerifyPhoneDto dto)
        {
            if (CurrentUserId is null)
            {
                return Unauthorized();
            }

            var result = await _service.VerifyPhoneAsync(CurrentUserId, dto);

            if (!result.IsSuccess)
            {
                return BadRequest(new { errors = result.Errors });
            }

            return Ok(new { message = "شماره موبایل با موفقیت تایید شد" });
        }
    }
}
