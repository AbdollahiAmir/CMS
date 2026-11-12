using BackEnd.Data;
using BackEnd.IServices;
using BackEnd.Models;
using Microsoft.AspNetCore.Identity;

namespace BackEnd.Services
{
    public class IdentitySeeder : IIdentitySeeder
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly IConfiguration _config;

        public IdentitySeeder(RoleManager<IdentityRole> roleManager, UserManager<AppUser> userManager, IConfiguration config)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _config = config;
        }

        public async Task SeedAsync()
        {
            foreach (var role in AppRoles.All)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var name = _config["Seed:AdminUserName"];
            var password = _config["Seed:AdminPassword"];
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("Seed:نام‌کاربری ادمین و رمز عبور ادمین باید کانفیگ شده باشند");
            }

            var admin = await _userManager.FindByNameAsync(name);
            if (admin == null)
            {
                admin = new AppUser
                {
                    UserName = name
                };

                var result = await _userManager.CreateAsync(admin, password);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"ایجاد اکانت ادمین با شکست مواجه شد: {errors}");
                }
            }

            if (!await _userManager.IsInRoleAsync(admin, AppRoles.Admin))
            {
                await _userManager.AddToRoleAsync(admin, AppRoles.Admin);
            }
        }
    }
}