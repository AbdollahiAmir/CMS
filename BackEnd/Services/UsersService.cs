using BackEnd.Authorization;
using BackEnd.Data;
using BackEnd.DTOs.Common;
using BackEnd.DTOs.Users;
using BackEnd.IServices;
using BackEnd.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Services
{
    public class UsersService : IUsersService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;
        private readonly IRefreshTokenService _refreshTokens;

        public UsersService(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            AppDbContext context,
            IRefreshTokenService refreshTokens)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _refreshTokens = refreshTokens;
        }

        private async Task<bool> ExceedsActorAsync(IEnumerable<string> roleNames, Actor actor)
        {
            if (actor.IsAdmin)
            {
                return false;
            }

            foreach (var roleName in roleNames)
            {
                if (roleName == AppRoles.Admin)
                {
                    return true;
                }

                var role = await _roleManager.FindByNameAsync(roleName);
                if (role is null)
                {
                    continue;
                }

                var claims = await _roleManager.GetClaimsAsync(role);
                var permissions = claims.Where(c => c.Type == AppPermissions.ClaimType).Select(c => c.Value);

                if (!actor.HasAll(permissions))
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<PagedResult<UserDto>> GetAllAsync(UserQueryDto query)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);

            var usersQuery = _userManager.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                usersQuery = usersQuery.Where(u => u.UserName != null && u.UserName.Contains(search));
            }

            var total = await usersQuery.CountAsync();

            var users = await usersQuery
                .OrderBy(u => u.UserName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new { u.Id, u.UserName })
                .ToListAsync();

            var userIds = users.Select(u => u.Id).ToList();

            var roleRows = await (from ur in _context.UserRoles.AsNoTracking().Where(x => userIds.Contains(x.UserId))
                                  join r in _context.Roles.AsNoTracking() on ur.RoleId equals r.Id
                                  select new { ur.UserId, RoleName = r.Name }).ToListAsync();

            var rolesByUser = roleRows.ToLookup(x => x.UserId, x => x.RoleName ?? string.Empty);

            return new PagedResult<UserDto>
            {
                Items = users.Select(u => new UserDto
                {
                    Id = u.Id,
                    UserName = u.UserName ?? string.Empty,
                    Roles = rolesByUser[u.Id].ToArray()
                }).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            };
        }

        public async Task<UserDto?> GetByIdAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null)
            {
                return null;
            }

            var roles = await _userManager.GetRolesAsync(user);

            return new UserDto
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Roles = roles.ToArray()
            };
        }

        public async Task<UserResult> EditAsync(string id, EditUserDto dto, Actor actor)
        {
            if (!await _roleManager.RoleExistsAsync(dto.Role))
            {
                return UserResult.Fail("نقش نامعتبر");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user is null)
            {
                return UserResult.NotFoundResult();
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var roleChanges = !currentRoles.Contains(dto.Role) || currentRoles.Count > 1;

            if (roleChanges && id == actor.Id)
            {
                return UserResult.ForbiddenResult("شما نمیتوانید نقش خودتان را تغییر دهید");
            }

            if (roleChanges && (await ExceedsActorAsync(currentRoles, actor) || await ExceedsActorAsync([dto.Role], actor)))
            {
                return UserResult.ForbiddenResult("شما اجازه‌ی این تغییر نقش را ندارید");
            }

            if (currentRoles.Contains(AppRoles.Admin) && dto.Role != AppRoles.Admin)
            {
                var admins = await _userManager.GetUsersInRoleAsync(AppRoles.Admin);

                if (admins.Count <= 1)
                {
                    return UserResult.Fail("آخرین اکانت ادمین نمیتواند برکنار شود.");
                }
            }

            if (!roleChanges && await ExceedsActorAsync(currentRoles, actor))
            {
                return UserResult.ForbiddenResult("شما اجازه‌ی ویرایش این کاربر را ندارید");
            }

            user.UserName = dto.UserName.Trim();

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return UserResult.Fail(updateResult.Errors.Select(e => e.Description));
            }

            if (roleChanges)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    return UserResult.Fail(removeResult.Errors.Select(e => e.Description));
                }

                var addResult = await _userManager.AddToRoleAsync(user, dto.Role);
                if (!addResult.Succeeded)
                {
                    return UserResult.Fail(addResult.Errors.Select(e => e.Description));
                }
            }

            await _refreshTokens.RevokeAllAsync(user.Id);

            return UserResult.Ok();
        }

        public async Task<UserResult> DeleteAsync(string id, Actor actor)
        {
            if (id == actor.Id)
            {
                return UserResult.Fail("شما نمیتوانید از اینجا اکانت خود را حذف کنید");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user is null)
            {
                return UserResult.NotFoundResult();
            }

            var roles = await _userManager.GetRolesAsync(user);

            if (await ExceedsActorAsync(roles, actor))
            {
                return UserResult.ForbiddenResult("شما اجازه‌ی حذف این کاربر را ندارید");
            }

            if (roles.Contains(AppRoles.Admin))
            {
                var admins = await _userManager.GetUsersInRoleAsync(AppRoles.Admin);

                if (admins.Count <= 1)
                {
                    return UserResult.Fail("آخرین اکانت ادمین نمیتواند حذف شود.");
                }
            }

            if (await _context.Orders.AnyAsync(o => o.UserId == id))
            {
                return UserResult.Fail("این کاربر سفارش ثبت‌شده دارد و قابل حذف نیست");
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return UserResult.Fail(result.Errors.Select(e => e.Description));
            }

            return UserResult.Ok();
        }
    }
}