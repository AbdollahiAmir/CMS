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
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _config;

        public AuthService(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration config)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _config = config;
        }

        private async Task<IEnumerable<string>> GetPermissionsAsync(IEnumerable<string> roleNames)
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

        private async Task<(string Token, DateTime Expires)> GenerateToken(AppUser user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName!),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            foreach (var permission in await GetPermissionsAsync(roles))
            {
                claims.Add(new Claim(AppPermissions.ClaimType, permission));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(double.Parse(_config["Jwt:ExpiryMinutes"]!));
            var jwt = new JwtSecurityToken(issuer: _config["Jwt:Issuer"], audience: _config["Jwt:Audience"], claims: claims, expires: expires, signingCredentials: credentials);

            return (new JwtSecurityTokenHandler().WriteToken(jwt), expires);
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
            var user = await _userManager.FindByNameAsync(dto.UserName);
            if (user == null)
            {
                return new AuthResult
                {
                    IsSuccess = false,
                    Errors = ["نام‌کاربری یا رمز عبور اشتباه است"]
                };
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                return new AuthResult
                {
                    IsSuccess = false,
                    Errors = ["حساب کاربری موقتا قفل شده است، لطفا چند دقیقه دیگر امتحان کنید"]
                };
            }

            if (!await _userManager.CheckPasswordAsync(user, dto.Password))
            {
                await _userManager.AccessFailedAsync(user);

                if (await _userManager.IsLockedOutAsync(user))
                {
                    return new AuthResult
                    {
                        IsSuccess = false,
                        Errors = ["تلاش های زیاد، حساب کاربری موقتا قفل میشود، لطفا 15 دقیقه دیگر امتحان کنید"]
                    };
                }

                return new AuthResult
                {
                    IsSuccess = false,
                    Errors = ["نام‌کاربری یا رمز عبور اشتباه است"]
                };
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            var (token, expires) = await GenerateToken(user);

            return new AuthResult
            {
                IsSuccess = true,
                Token = token,
                ExpiresAt = expires
            };
        }

        public async Task<AuthResult> ChangePasswordAsync(string userId, ChangePasswordDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new AuthResult
                {
                    IsSuccess = false,
                    Errors = ["کاربر یافت نشد"]
                };
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

            return new AuthResult { IsSuccess = true };
        }
    }
}