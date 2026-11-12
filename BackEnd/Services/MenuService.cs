using BackEnd.Data;
using BackEnd.DTOs.Menus;
using BackEnd.Helpers;
using BackEnd.IServices;
using BackEnd.Models;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Services
{
    public class MenuService : IMenuService
    {
        private const int MaxDepth = 3;
        private const int MaxItems = 100;

        private readonly AppDbContext _context;

        public MenuService(AppDbContext context)
        {
            _context = context;
        }

        private static List<MenuItemDto> BuildTree(List<MenuItem> items)
        {
            var byParent = items
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.Id)
                .GroupBy(i => i.ParentId ?? 0)
                .ToDictionary(g => g.Key, g => g.ToList());

            List<MenuItemDto> Build(int parentId)
            {
                if (!byParent.TryGetValue(parentId, out var children))
                {
                    return new List<MenuItemDto>();
                }

                return children
                    .Select(i => new MenuItemDto
                    {
                        Id = i.Id,
                        Title = i.Title,
                        Url = i.Url,
                        OpenInNewTab = i.OpenInNewTab,
                        Children = Build(i.Id)
                    })
                    .ToList();
            }

            return Build(0);
        }

        private async Task<MenuDto?> LoadAsync(Func<IQueryable<Menu>, IQueryable<Menu>> filter)
        {
            var menu = await filter(_context.Menus.AsNoTracking()).FirstOrDefaultAsync();

            if (menu is null)
            {
                return null;
            }

            var items = await _context.MenuItems
                .AsNoTracking()
                .Where(i => i.MenuId == menu.Id)
                .ToListAsync();

            return new MenuDto
            {
                Id = menu.Id,
                Key = menu.Key,
                Name = menu.Name,
                UpdatedAt = menu.UpdatedAt,
                Items = BuildTree(items)
            };
        }

        public async Task<List<MenuListItemDto>> GetAllAsync()
        {
            return await _context.Menus
                .AsNoTracking()
                .OrderBy(m => m.Key)
                .Select(m => new MenuListItemDto
                {
                    Id = m.Id,
                    Key = m.Key,
                    Name = m.Name,
                    ItemsCount = m.Items.Count,
                    UpdatedAt = m.UpdatedAt
                })
                .ToListAsync();
        }

        public Task<MenuDto?> GetByIdAsync(int id)
        {
            return LoadAsync(q => q.Where(m => m.Id == id));
        }

        public Task<MenuDto?> GetByKeyAsync(string key)
        {
            var normalized = key.Trim().ToLowerInvariant();

            return LoadAsync(q => q.Where(m => m.Key == normalized));
        }

        public async Task<MenuDto> CreateAsync(CreateMenuDto dto)
        {
            var key = dto.Key.Trim().ToLowerInvariant();
            var name = dto.Name.Trim();

            if (name.Length == 0)
            {
                throw new ArgumentException("نام منو الزامی است");
            }

            if (await _context.Menus.AnyAsync(m => m.Key == key))
            {
                throw new ArgumentException("منویی با این کلید وجود دارد");
            }

            var menu = new Menu
            {
                Key = key,
                Name = name,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Menus.Add(menu);
            await _context.SaveChangesAsync();

            return (await GetByIdAsync(menu.Id))!;
        }

        public async Task<MenuDto?> PatchAsync(int id, PatchMenuDto dto)
        {
            var menu = await _context.Menus.FirstOrDefaultAsync(m => m.Id == id);

            if (menu is null)
            {
                return null;
            }

            if (dto.Name is not null)
            {
                var name = dto.Name.Trim();

                if (name.Length == 0)
                {
                    throw new ArgumentException("نام منو نمی‌تواند خالی باشد");
                }

                menu.Name = name;
                menu.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return await GetByIdAsync(id);
        }

        public async Task<MenuDto?> ReplaceItemsAsync(int id, SaveMenuItemsDto dto)
        {
            var menu = await _context.Menus.FirstOrDefaultAsync(m => m.Id == id);

            if (menu is null)
            {
                return null;
            }

            var count = 0;

            List<MenuItem> Convert(List<MenuItemInputDto>? inputs, int depth)
            {
                var result = new List<MenuItem>();

                if (inputs is null)
                {
                    return result;
                }

                if (depth > MaxDepth)
                {
                    throw new ArgumentException($"عمق منو نمی‌تواند بیشتر از {MaxDepth} سطح باشد");
                }

                for (var index = 0; index < inputs.Count; index++)
                {
                    var input = inputs[index];

                    count++;

                    if (count > MaxItems)
                    {
                        throw new ArgumentException($"تعداد آیتم‌های منو نمی‌تواند بیشتر از {MaxItems} باشد");
                    }

                    var title = input.Title?.Trim() ?? string.Empty;

                    if (title.Length == 0)
                    {
                        throw new ArgumentException("عنوان آیتم منو نمی‌تواند خالی باشد");
                    }

                    var url = UrlValidator.Normalize(input.Url, $"آدرس آیتم «{title}»");

                    if (url is null)
                    {
                        throw new ArgumentException($"آدرس آیتم «{title}» الزامی است");
                    }

                    var item = new MenuItem
                    {
                        Title = title,
                        Url = url,
                        OpenInNewTab = input.OpenInNewTab,
                        SortOrder = index
                    };

                    foreach (var child in Convert(input.Children, depth + 1))
                    {
                        item.Children.Add(child);
                    }

                    result.Add(item);
                }

                return result;
            }

            var roots = Convert(dto.Items, 1);

            await using var transaction = await _context.Database.BeginTransactionAsync();

            await _context.MenuItems
                .Where(i => i.MenuId == id)
                .ExecuteDeleteAsync();

            foreach (var root in roots)
            {
                root.MenuId = id;
                AssignMenu(root, id);
                _context.MenuItems.Add(root);
            }

            menu.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return await GetByIdAsync(id);
        }

        private static void AssignMenu(MenuItem item, int menuId)
        {
            foreach (var child in item.Children)
            {
                child.MenuId = menuId;
                AssignMenu(child, menuId);
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var menu = await _context.Menus.FirstOrDefaultAsync(m => m.Id == id);

            if (menu is null)
            {
                return false;
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            await _context.MenuItems
                .Where(i => i.MenuId == id)
                .ExecuteDeleteAsync();

            _context.Menus.Remove(menu);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
    }
}
