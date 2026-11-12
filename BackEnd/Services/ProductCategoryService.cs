using BackEnd.Data;
using BackEnd.DTOs.Category;
using BackEnd.Helpers;
using BackEnd.IServices;
using BackEnd.Models;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Services
{
    public class ProductCategoryService : IProductCategoryService
    {
        private readonly AppDbContext _context;
        private readonly IFileService _fileService;

        public ProductCategoryService(AppDbContext context, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        private static string? Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static ProductCategoryDto Map(ProductCategory category)
        {
            return new ProductCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                Description = category.Description,
                SeoTitle = category.SeoTitle,
                MetaDescription = category.MetaDescription,
                HasImage = category.ImgPath != null,
                SortOrder = category.SortOrder,
                IsActive = category.IsActive,
                ParentId = category.ParentId
            };
        }

        private async Task EnsureValidParentAsync(int parentId, int? categoryId)
        {
            if (categoryId.HasValue && parentId == categoryId.Value)
            {
                throw new ArgumentException("یک دسته بندی نمیتواند زیرمجموعه‌ی خودش باشد");
            }

            var nodes = await _context.ProductCategories.AsNoTracking()
                .Select(c => new { c.Id, c.ParentId })
                .ToListAsync();

            if (!nodes.Any(n => n.Id == parentId))
            {
                throw new ArgumentException("دسته بندی والد وجود ندارد");
            }

            if (categoryId.HasValue)
            {
                var descendants = CategoryTreeHelper.GetDescendantIds(nodes.Select(n => (n.Id, n.ParentId)), categoryId.Value);
                if (descendants.Contains(parentId))
                {
                    throw new ArgumentException("دسته بندی والد نمیتواند یکی از زیرمجموعه‌های همین دسته بندی باشد");
                }
            }
        }

        public async Task<List<ProductCategoryDto>> GetTreeAsync(bool includeInactive)
        {
            var query = _context.ProductCategories.AsNoTracking();

            if (!includeInactive)
            {
                query = query.Where(c => c.IsActive);
            }

            var all = await query.OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync();

            var nodes = all.ToDictionary(c => c.Id, Map);
            var roots = new List<ProductCategoryDto>();

            foreach (var category in all)
            {
                if (category.ParentId is null)
                {
                    roots.Add(nodes[category.Id]);
                }
                else if (nodes.TryGetValue(category.ParentId.Value, out var parent))
                {
                    parent.Children.Add(nodes[category.Id]);
                }
            }

            return roots;
        }

        public async Task<ProductCategoryDto?> GetByIdAsync(int id)
        {
            var category = await _context.ProductCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

            return category is null ? null : Map(category);
        }

        public async Task<ProductCategoryDto> CreateAsync(CreateProductCategoryDto dto)
        {
            var name = dto.Name.Trim();
            if (name.Length == 0)
            {
                throw new ArgumentException("نام دسته بندی الزامی است");
            }

            if (dto.ParentId.HasValue)
            {
                await EnsureValidParentAsync(dto.ParentId.Value, null);
            }

            var category = new ProductCategory
            {
                Name = name,
                Slug = await SlugHelper.ResolveAsync(dto.Slug, name, s => _context.ProductCategories.AnyAsync(c => c.Slug == s)),
                Description = Clean(dto.Description),
                SeoTitle = Clean(dto.SeoTitle),
                MetaDescription = Clean(dto.MetaDescription),
                ParentId = dto.ParentId,
                SortOrder = dto.SortOrder,
                IsActive = dto.IsActive
            };

            if (dto.ImgFile is { Length: > 0 })
            {
                category.ImgPath = await _fileService.SaveCategoryImageAsync(dto.ImgFile);
            }

            _context.ProductCategories.Add(category);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                await _fileService.DeleteCategoryImageAsync(category.ImgPath);
                throw;
            }

            return Map(category);
        }

        public async Task<ProductCategoryDto?> PatchAsync(int id, PatchProductCategoryDto dto)
        {
            var category = await _context.ProductCategories.FirstOrDefaultAsync(c => c.Id == id);
            if (category is null)
            {
                return null;
            }

            if (dto.Name is not null)
            {
                var name = dto.Name.Trim();
                if (name.Length == 0)
                {
                    throw new ArgumentException("نام دسته بندی نمیتواند خالی باشد");
                }

                category.Name = name;
            }

            if (dto.Slug is not null && SlugHelper.Generate(dto.Slug) != category.Slug)
            {
                category.Slug = await SlugHelper.ResolveAsync(dto.Slug, category.Name, s => _context.ProductCategories.AnyAsync(c => c.Slug == s && c.Id != id));
            }

            if (dto.Description is not null)
            {
                category.Description = Clean(dto.Description);
            }

            if (dto.SeoTitle is not null)
            {
                category.SeoTitle = Clean(dto.SeoTitle);
            }

            if (dto.MetaDescription is not null)
            {
                category.MetaDescription = Clean(dto.MetaDescription);
            }

            if (dto.SortOrder.HasValue)
            {
                category.SortOrder = dto.SortOrder.Value;
            }

            if (dto.IsActive.HasValue)
            {
                category.IsActive = dto.IsActive.Value;
            }

            if (dto.ParentId.HasValue)
            {
                if (dto.ParentId.Value == 0)
                {
                    category.ParentId = null;
                }
                else
                {
                    await EnsureValidParentAsync(dto.ParentId.Value, id);
                    category.ParentId = dto.ParentId.Value;
                }
            }

            var oldImagePath = category.ImgPath;
            string? newImagePath = null;

            if (dto.ImgFile is { Length: > 0 })
            {
                newImagePath = await _fileService.SaveCategoryImageAsync(dto.ImgFile);
                category.ImgPath = newImagePath;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                if (newImagePath is not null)
                {
                    await _fileService.DeleteCategoryImageAsync(newImagePath);
                }

                throw;
            }

            if (newImagePath is not null)
            {
                await _fileService.DeleteCategoryImageAsync(oldImagePath);
            }

            return Map(category);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var category = await _context.ProductCategories.FirstOrDefaultAsync(c => c.Id == id);
            if (category is null)
            {
                return false;
            }

            if (await _context.ProductCategories.AnyAsync(c => c.ParentId == id))
            {
                throw new ArgumentException("این دسته بندی زیرمجموعه دارد، ابتدا زیرمجموعه‌ها را حذف یا جابه‌جا کنید");
            }

            var imagePath = category.ImgPath;

            var products = await _context.Products
                .Include(p => p.Categories)
                .Where(p => p.Categories.Any(c => c.Id == id))
                .ToListAsync();

            var deactivatedIds = new List<int>();

            foreach (var product in products)
            {
                var link = product.Categories.First(c => c.Id == id);
                product.Categories.Remove(link);

                if (product.IsActive && product.Categories.Count == 0)
                {
                    product.IsActive = false;
                    deactivatedIds.Add(product.Id);
                }
            }

            if (deactivatedIds.Count > 0)
            {
                var cartItems = await _context.CartItems.Where(i => deactivatedIds.Contains(i.ProductId)).ToListAsync();
                _context.CartItems.RemoveRange(cartItems);
            }

            _context.ProductCategories.Remove(category);

            await _context.SaveChangesAsync();

            await _fileService.DeleteCategoryImageAsync(imagePath);

            return true;
        }

        public async Task<(Stream Stream, string ContentType)?> GetCategoryImageAsync(int id, bool thumbnail = false)
        {
            var category = await _context.ProductCategories.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
            if (category is null)
            {
                return null;
            }

            return await _fileService.OpenCategoryImageAsync(category.ImgPath, thumbnail);
        }
    }
}