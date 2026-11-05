using BackEnd.Authorization;
using BackEnd.Data;
using BackEnd.DTOs.Roles;
using BackEnd.IServices;
using BackEnd.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BackEnd.Services
{
    public class RolesService : IRolesService
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _context;

        public RolesService(RoleManager<IdentityRole> roleManager, UserManager<AppUser> userManager, AppDbContext context)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _context = context;
        }

        private static bool IsSystemRole(string? name)
        {
            return name == AppRoles.Admin || name == AppRoles.Customer;
        }

        private static RoleResult Fail(params string[] errors)
        {
            return new RoleResult { IsSuccess = false, Errors = errors };
        }

        private static RoleResult ForbiddenResult(string message)
        {
            return new RoleResult { IsSuccess = false, Forbidden = true, Errors = [message] };
        }

        private static RoleResult NotFoundResult()
        {
            return new RoleResult { IsSuccess = false, NotFound = true, Errors = ["نقش یافت نشد"] };
        }

        private static RoleDto MapRole(IdentityRole role, IEnumerable<string> permissions)
        {
            return new RoleDto
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                IsSystem = IsSystemRole(role.Name),
                Permissions = role.Name == AppRoles.Admin
                    ? AppPermissions.All.ToArray()
                    : permissions.OrderBy(p => p).ToArray()
            };
        }

        private async Task<string[]> GetRolePermissionsAsync(IdentityRole role)
        {
            var claims = await _roleManager.GetClaimsAsync(role);

            return claims
                .Where(c => c.Type == AppPermissions.ClaimType)
                .Select(c => c.Value)
                .ToArray();
        }

        private static string? ValidatePermissions(IEnumerable<string> permissions)
        {
            var invalid = permissions.Where(p => !AppPermissions.All.Contains(p)).Distinct().ToList();

            return invalid.Count == 0 ? null : $"مجوزهای نامعتبر: {string.Join(", ", invalid)}";
        }

        private async Task<IEnumerable<string>?> SyncPermissionsAsync(IdentityRole role, IEnumerable<string> permissions)
        {
            var wanted = permissions.Distinct().ToHashSet();
            var currentClaims = (await _roleManager.GetClaimsAsync(role))
                .Where(c => c.Type == AppPermissions.ClaimType)
                .ToList();

            foreach (var claim in currentClaims.Where(c => !wanted.Contains(c.Value)))
            {
                var removeResult = await _roleManager.RemoveClaimAsync(role, claim);
                if (!removeResult.Succeeded)
                {
                    return removeResult.Errors.Select(e => e.Description);
                }
            }

            var existing = currentClaims.Select(c => c.Value).ToHashSet();

            foreach (var permission in wanted.Where(p => !existing.Contains(p)))
            {
                var addResult = await _roleManager.AddClaimAsync(role, new Claim(AppPermissions.ClaimType, permission));
                if (!addResult.Succeeded)
                {
                    return addResult.Errors.Select(e => e.Description);
                }
            }

            return null;
        }

        public IReadOnlyList<string> GetPermissions()
        {
            return AppPermissions.All;
        }

        public async Task<IReadOnlyList<RoleDto>> GetAllAsync()
        {
            var roles = await _roleManager.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync();

            var claimRows = await _context.RoleClaims.AsNoTracking()
                .Where(c => c.ClaimType == AppPermissions.ClaimType)
                .Select(c => new { c.RoleId, c.ClaimValue })
                .ToListAsync();

            var permissionsByRole = claimRows.ToLookup(c => c.RoleId, c => c.ClaimValue ?? string.Empty);

            return roles.Select(r => MapRole(r, permissionsByRole[r.Id])).ToList();
        }

        public async Task<RoleDto?> GetByIdAsync(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role is null)
            {
                return null;
            }

            return MapRole(role, await GetRolePermissionsAsync(role));
        }

        public async Task<RoleResult> CreateAsync(SaveRoleDto dto, Actor actor)
        {
            var name = dto.Name.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                return Fail("نام نقش الزامی است");
            }

            if (IsSystemRole(name))
            {
                return Fail("این نام برای نقش‌های سیستمی رزرو شده است");
            }

            var permissionError = ValidatePermissions(dto.Permissions);
            if (permissionError is not null)
            {
                return Fail(permissionError);
            }

            if (!actor.HasAll(dto.Permissions))
            {
                return ForbiddenResult("نمیتوانید مجوزی را بدهید که خودتان ندارید");
            }

            var role = new IdentityRole(name);

            var createResult = await _roleManager.CreateAsync(role);
            if (!createResult.Succeeded)
            {
                return new RoleResult { IsSuccess = false, Errors = createResult.Errors.Select(e => e.Description) };
            }

            var syncErrors = await SyncPermissionsAsync(role, dto.Permissions);
            if (syncErrors is not null)
            {
                await _roleManager.DeleteAsync(role);
                return new RoleResult { IsSuccess = false, Errors = syncErrors };
            }

            return new RoleResult { IsSuccess = true, Role = MapRole(role, await GetRolePermissionsAsync(role)) };
        }

        public async Task<RoleResult> UpdateAsync(string id, SaveRoleDto dto, Actor actor)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role is null)
            {
                return NotFoundResult();
            }

            if (IsSystemRole(role.Name))
            {
                return Fail("نقش‌های سیستمی قابل ویرایش نیستند");
            }

            var name = dto.Name.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                return Fail("نام نقش الزامی است");
            }

            if (IsSystemRole(name))
            {
                return Fail("این نام برای نقش‌های سیستمی رزرو شده است");
            }

            var permissionError = ValidatePermissions(dto.Permissions);
            if (permissionError is not null)
            {
                return Fail(permissionError);
            }

            var existingPermissions = await GetRolePermissionsAsync(role);

            if (!actor.HasAll(existingPermissions))
            {
                return ForbiddenResult("نمیتوانید نقشی را که مجوزهای بیشتری از شما دارد ویرایش کنید");
            }

            if (!actor.HasAll(dto.Permissions.Except(existingPermissions)))
            {
                return ForbiddenResult("نمیتوانید مجوزی را بدهید که خودتان ندارید");
            }

            if (role.Name != name)
            {
                var renameResult = await _roleManager.SetRoleNameAsync(role, name);
                if (!renameResult.Succeeded)
                {
                    return new RoleResult { IsSuccess = false, Errors = renameResult.Errors.Select(e => e.Description) };
                }

                var updateResult = await _roleManager.UpdateAsync(role);
                if (!updateResult.Succeeded)
                {
                    return new RoleResult { IsSuccess = false, Errors = updateResult.Errors.Select(e => e.Description) };
                }
            }

            var syncErrors = await SyncPermissionsAsync(role, dto.Permissions);
            if (syncErrors is not null)
            {
                return new RoleResult { IsSuccess = false, Errors = syncErrors };
            }

            return new RoleResult { IsSuccess = true, Role = MapRole(role, await GetRolePermissionsAsync(role)) };
        }

        public async Task<RoleResult> DeleteAsync(string id, Actor actor)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role is null)
            {
                return NotFoundResult();
            }

            if (IsSystemRole(role.Name))
            {
                return Fail("نقش‌های سیستمی قابل حذف نیستند");
            }

            if (!actor.HasAll(await GetRolePermissionsAsync(role)))
            {
                return ForbiddenResult("نمیتوانید نقشی را که مجوزهای بیشتری از شما دارد حذف کنید");
            }

            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
            if (usersInRole.Count > 0)
            {
                return Fail("این نقش به کاربرانی اختصاص داده شده و قابل حذف نیست");
            }

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                return new RoleResult { IsSuccess = false, Errors = result.Errors.Select(e => e.Description) };
            }

            return new RoleResult { IsSuccess = true };
        }
    }
}