using BackEnd.Data;
using BackEnd.DTOs.Auth;
using BackEnd.Helpers;
using BackEnd.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace BackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _service;
        private readonly IConfiguration _config;

        public AuthController(IAuthService service, IConfiguration config)
        {
            _service = service;
            _config = config;
        }

        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var result = await _service.RegisterAsync(dto);

            if (!result.IsSuccess)
            {
                return BadRequest(new { errors = result.Errors });
            }

            return Ok(new { message = "ثبت نام با موفقیت انجام شد" });
        }

        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var result = await _service.LoginAsync(dto);
            if (!result.IsSuccess)
            {
                return Unauthorized(new { errors = result.Errors });
            }

            if (string.IsNullOrEmpty(result.Token) || result.ExpiresAt is null)
            {
                return Unauthorized(new { message = "احراز هویت با شکست مواجه شد." });
            }

            Response.Cookies.Append(AuthCookie.Name, result.Token, AuthCookie.Create(_config, new DateTimeOffset(result.ExpiresAt.Value)));

            return Ok(new
            {
                message = "ورود با موفقیت انجام شد.",
                expiresAt = result.ExpiresAt
            });
        }

        [AllowAnonymous]
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete(AuthCookie.Name, AuthCookie.Create(_config, null));

            return Ok(new { message = "خروج با موفقیت انجام شد" });
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = User.Identity?.Name;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userName))
            {
                return Unauthorized();
            }

            var roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray();
            var permissions = User.FindAll(AppPermissions.ClaimType).Select(claim => claim.Value).ToArray();

            return Ok(new
            {
                userId,
                userName,
                roles,
                permissions
            });
        }

        [Authorize]
        [EnableRateLimiting("auth")]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _service.ChangePasswordAsync(userId, dto);

            if (!result.IsSuccess)
            {
                return BadRequest(new { errors = result.Errors });
            }

            return Ok(new { message = "تغییر رمز با موفقیت انجام شد" });
        }
    }
}