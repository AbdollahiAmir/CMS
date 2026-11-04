using BackEnd.Data;
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
        private readonly AppDbContext _context;

        public UsersService(UserManager<AppUser> userManager, AppDbContext context)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<UserDto> MapUserAsync(AppUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            return new UserDto
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Roles = roles.ToArray()
            };
        }

        public async Task<IReadOnlyList<UserDto>> GetAllAsync()
        {
            var users = _userManager.Users.ToList();

            var result = new List<UserDto>();

            foreach (var user in users)
            {
                result.Add(await MapUserAsync(user));
            }

            return result;
        }

        public async Task<UserDto?> GetByIdAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if(user is null)
            {
                return null;
            }

            return await MapUserAsync(user);
        }

        public async Task<(bool Succeeded, IEnumerable<string> Errors)> EditAsync(string id, EditUserDto dto)
        {
            if (dto.Role != AppRoles.Admin && dto.Role != AppRoles.Seller && dto.Role != AppRoles.Customer)
            {
                return (false, new[] { "نقش نامعتبر" });
            }

            var user = await _userManager.FindByIdAsync(id);
            if(user is null)
            {
                return (false, new[] { "کاربر یافت نشد" });
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var isAdmin = currentRoles.Contains(AppRoles.Admin);
            var willBeAdmin = dto.Role == AppRoles.Admin;

            if (isAdmin && !willBeAdmin)
            {
                var admins = await _userManager.GetUsersInRoleAsync(AppRoles.Admin);

                if (admins.Count <= 1)
                {
                    return (false, new[] { "آخرین اکانت ادمین نمیتواند برکنار شود." });
                }
            }

            user.UserName = dto.UserName.Trim();

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return (false, updateResult.Errors.Select(e => e.Description));
            }

            if (!currentRoles.Contains(dto.Role))
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);

                if (!removeResult.Succeeded)
                {
                    return (false, removeResult.Errors.Select(e => e.Description));
                }

                var addResult = await _userManager.AddToRoleAsync(user, dto.Role);

                if (!addResult.Succeeded)
                {
                    return (false, addResult.Errors.Select(e => e.Description));
                }
            }

            return (true, Array.Empty<string>());
        }

        public async Task<(bool Succeeded, IEnumerable<string> Errors)> DeleteAsync(string id, string? currentUserId)
        {
            if (id == currentUserId)
            {
                return (false, new[] { "شما نمیتواند از اینجا اکانت خود را حذف کنید" });
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return (false, new[] { "کاربر یافت نشد" });
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains(AppRoles.Admin))
            {
                var admins = await _userManager.GetUsersInRoleAsync(AppRoles.Admin);

                if (admins.Count <= 1)
                {
                    return (false, new[] { "آخرین اکانت ادمین نمیتواند حذف شود." });
                }
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var now = DateTime.UtcNow;

            var products = await _context.Products.Where(p => p.SellerId == id).ToListAsync();
            foreach (var product in products)
            {
                product.SellerId = null;
                product.IsApproved = false;
            }

            await _context.SaveChangesAsync();

            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                await transaction.RollbackAsync();
                return (false, result.Errors.Select(e => e.Description));
            }

            await transaction.CommitAsync();

            return (true, Array.Empty<string>());
        }
    }
}