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

        private static DateTimeOffset AsUtc(DateTime value)
        {
            return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
        }

        private void SetAuthCookies(AuthResult result)
        {
            Response.Cookies.Append(
                AuthCookie.Name,
                result.Token!,
                AuthCookie.Create(_config, AsUtc(result.ExpiresAt!.Value)));

            Response.Cookies.Append(
                AuthCookie.RefreshName,
                result.RefreshToken!,
                AuthCookie.Create(_config, AsUtc(result.RefreshExpiresAt!.Value), AuthCookie.RefreshPath));
        }

        private void ClearAuthCookies()
        {
            Response.Cookies.Delete(AuthCookie.Name, AuthCookie.Create(_config, null));
            Response.Cookies.Delete(AuthCookie.RefreshName, AuthCookie.Create(_config, null, AuthCookie.RefreshPath));
        }

        private static bool HasFullSession(AuthResult result)
        {
            return !string.IsNullOrEmpty(result.Token)
                && !string.IsNullOrEmpty(result.RefreshToken)
                && result.ExpiresAt is not null
                && result.RefreshExpiresAt is not null;
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

            if (!HasFullSession(result))
            {
                return Unauthorized(new { message = "احراز هویت با شکست مواجه شد." });
            }

            SetAuthCookies(result);

            return Ok(new
            {
                message = "ورود با موفقیت انجام شد.",
                expiresAt = result.ExpiresAt,
                user = result.User
            });
        }

        [AllowAnonymous]
        [EnableRateLimiting("refresh")]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var result = await _service.RefreshAsync(Request.Cookies[AuthCookie.RefreshName]);

            if (!result.IsSuccess || !HasFullSession(result))
            {
                ClearAuthCookies();

                return Unauthorized(new { errors = result.Errors });
            }

            SetAuthCookies(result);

            return Ok(new
            {
                expiresAt = result.ExpiresAt,
                user = result.User
            });
        }

        [AllowAnonymous]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _service.LogoutAsync(Request.Cookies[AuthCookie.RefreshName]);

            ClearAuthCookies();

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

            ClearAuthCookies();

            return Ok(new { message = "تغییر رمز با موفقیت انجام شد، دوباره وارد شوید" });
        }
    }
}