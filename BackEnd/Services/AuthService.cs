using BackEnd.Data;
using BackEnd.DTOs.Auth;
using BackEnd.IServices;
using BackEnd.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BackEnd.Services
{
    public class AuthService : IAuthService
    {
        private static readonly AppUser TimingDummyUser = new() { UserName = "timing-dummy" };

        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _config;
        private readonly IRefreshTokenService _refreshTokens;

        public AuthService(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration config,
            IRefreshTokenService refreshTokens)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _config = config;
            _refreshTokens = refreshTokens;
        }

        private static AuthResult Fail(params string[] errors)
        {
            return new AuthResult { IsSuccess = false, Errors = errors };
        }

        private async Task<IReadOnlyCollection<string>> GetPermissionsAsync(IEnumerable<string> roleNames)
        {
            if (roleNames.Contains(AppRoles.Admin))
            {
                return AppPermissions.All;
            }

            var permissions = new HashSet<string>();

            foreach (var roleName in roleNames)
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role is null)
                {
                    continue;
                }

                var claims = await _roleManager.GetClaimsAsync(role);
                foreach (var claim in claims.Where(c => c.Type == AppPermissions.ClaimType))
                {
                    permissions.Add(claim.Value);
                }
            }

            return permissions;
        }

        private async Task<(string Token, DateTime Expires, AuthUserDto User)> GenerateAccessTokenAsync(AppUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var permissions = await GetPermissionsAsync(roles);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName!),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
            claims.AddRange(permissions.Select(permission => new Claim(AppPermissions.ClaimType, permission)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(_config.GetValue("Jwt:ExpiryMinutes", 15d));

            var jwt = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: credentials);

            var profile = new AuthUserDto
            {
                UserId = user.Id,
                UserName = user.UserName!,
                Roles = roles.ToArray(),
                Permissions = permissions.ToArray()
            };

            return (new JwtSecurityTokenHandler().WriteToken(jwt), expires, profile);
        }

        private async Task<AuthResult> BuildSessionAsync(AppUser user)
        {
            var access = await GenerateAccessTokenAsync(user);
            var refresh = await _refreshTokens.IssueAsync(user.Id);

            return new AuthResult
            {
                IsSuccess = true,
                Token = access.Token,
                ExpiresAt = access.Expires,
                RefreshToken = refresh.Token,
                RefreshExpiresAt = refresh.ExpiresAt,
                User = access.User
            };
        }

        public async Task<AuthResult> RegisterAsync(RegisterDto dto)
        {
            var user = new AppUser
            {
                UserName = dto.UserName
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                return new AuthResult
                {
                    IsSuccess = false,
                    Errors = result.Errors.Select(e => e.Description)
                };
            }

            await _userManager.AddToRoleAsync(user, AppRoles.Customer);

            return new AuthResult { IsSuccess = true };
        }

        public async Task<AuthResult> LoginAsync(LoginDto dto)
        {
            const string invalidCredentials = "نام‌کاربری یا رمز عبور اشتباه است";

            var user = await _userManager.FindByNameAsync(dto.UserName);
            if (user == null)
            {
                _userManager.PasswordHasher.HashPassword(TimingDummyUser, dto.Password);

                return Fail(invalidCredentials);
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                return Fail("حساب کاربری موقتا قفل شده است، لطفا چند دقیقه دیگر امتحان کنید");
            }

            if (!await _userManager.CheckPasswordAsync(user, dto.Password))
            {
                await _userManager.AccessFailedAsync(user);

                if (await _userManager.IsLockedOutAsync(user))
                {
                    return Fail("تلاش های زیاد، حساب کاربری موقتا قفل میشود، لطفا 15 دقیقه دیگر امتحان کنید");
                }

                return Fail(invalidCredentials);
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            return await BuildSessionAsync(user);
        }

        public async Task<AuthResult> RefreshAsync(string? refreshToken)
        {
            const string sessionExpired = "نشست شما منقضی شده است، دوباره وارد شوید";

            var rotation = await _refreshTokens.RotateAsync(refreshToken);

            if (!rotation.Succeeded || rotation.UserId is null || rotation.Token is null)
            {
                return Fail(sessionExpired);
            }

            var user = await _userManager.FindByIdAsync(rotation.UserId);

            if (user is null || await _userManager.IsLockedOutAsync(user))
            {
                await _refreshTokens.RevokeAllAsync(rotation.UserId);

                return Fail(sessionExpired);
            }

            var access = await GenerateAccessTokenAsync(user);

            return new AuthResult
            {
                IsSuccess = true,
                Token = access.Token,
                ExpiresAt = access.Expires,
                RefreshToken = rotation.Token.Token,
                RefreshExpiresAt = rotation.Token.ExpiresAt,
                User = access.User
            };
        }

        public async Task LogoutAsync(string? refreshToken)
        {
            await _refreshTokens.RevokeSessionAsync(refreshToken);
        }

        public async Task<AuthResult> ChangePasswordAsync(string? userId, ChangePasswordDto dto)
        {
            var user = userId is null ? null : await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Fail("کاربر یافت نشد");
            }

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
            {
                return new AuthResult
                {
                    IsSuccess = false,
                    Errors = result.Errors.Select(e => e.Description)
                };
            }

            await _refreshTokens.RevokeAllAsync(user.Id);

            return new AuthResult { IsSuccess = true };
        }
    }
}