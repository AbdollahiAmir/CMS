using BackEnd.Data;
using BackEnd.DTOs.PostCategories;
using BackEnd.Helpers;
using BackEnd.IServices;
using BackEnd.Models;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Services
{
    public class PostCategoryService : IPostCategoryService
    {
        private readonly AppDbContext _context;

        public PostCategoryService(AppDbContext context)
        {
            _context = context;
        }


        // تمیز کردن متن
        private static string? Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        // PostCategory => PostCategoryDto
        private static PostCategoryDto Map(PostCategory category)
        {
            return new PostCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                Description = category.Description,
                SeoTitle = category.SeoTitle,
                MetaDescription = category.MetaDescription,
                IsActive = category.IsActive,
                SortOrder = category.SortOrder,
                PostsCount = category.Posts.Count
            };
        }

        public async Task<List<PostCategoryDto>> GetAllAsync(bool includeInactive)
        {
            var query = _context.PostCategories.AsNoTracking().AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(c => c.IsActive);
            }

            return await query.OrderBy(c => c.SortOrder).ThenBy(c => c.Name).Select(c => new PostCategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    Description = c.Description,
                    SeoTitle = c.SeoTitle,
                    MetaDescription = c.MetaDescription,
                    IsActive = c.IsActive,
                    SortOrder = c.SortOrder,
                    PostsCount = c.Posts.Count
                }).ToListAsync();
        }

        public async Task<PostCategoryDto?> GetByIdAsync(int id)
        {
            var category = await _context.PostCategories.AsNoTracking().Include(c => c.Posts).FirstOrDefaultAsync(c => c.Id == id);

            return category is null ? null : Map(category);
        }

        public async Task<PostCategoryDto> CreateAsync(CreatePostCategoryDto dto)
        {
            var name = dto.Name.Trim();
            if (name.Length == 0)
            {
                throw new ArgumentException("نام دسته‌بندی الزامی است");
            }

            var category = new PostCategory
            {
                Name = name,
                Slug = await SlugHelper.ResolveAsync(dto.Slug, name, slug => _context.PostCategories.AnyAsync(c => c.Slug == slug)),
                Description = Clean(dto.Description),
                SeoTitle = Clean(dto.SeoTitle),
                MetaDescription = Clean(dto.MetaDescription),
                IsActive = dto.IsActive,
                SortOrder = dto.SortOrder
            };

            _context.PostCategories.Add(category);
            await _context.SaveChangesAsync();

            return Map(category);
        }

        public async Task<PostCategoryDto?> PatchAsync(int id, PatchPostCategoryDto dto)
        {
            var category = await _context.PostCategories.Include(c => c.Posts).FirstOrDefaultAsync(c => c.Id == id);
            if (category is null)
            {
                return null;
            }

            if (dto.Name is not null)
            {
                var name = dto.Name.Trim();
                if (name.Length == 0)
                {
                    throw new ArgumentException("نام دسته‌بندی نمی‌تواند خالی باشد");
                }

                category.Name = name;
                //category.Slug = await SlugHelper.ResolveAsync(dto.Slug, category.Name, slug => _context.PostCategories.AnyAsync(c => c.Slug == slug && c.Id != id));
            }

            if (dto.Slug is not null && SlugHelper.Generate(dto.Slug) != category.Slug)
            {
                category.Slug = await SlugHelper.ResolveAsync(dto.Slug, category.Name, slug => _context.PostCategories.AnyAsync(c => c.Slug == slug && c.Id != id));
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

            if (dto.IsActive.HasValue)
            {
                category.IsActive = dto.IsActive.Value;
            }

            if (dto.SortOrder.HasValue)
            {
                category.SortOrder = dto.SortOrder.Value;
            }

            await _context.SaveChangesAsync();

            return Map(category);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var category = await _context.PostCategories.FirstOrDefaultAsync(c => c.Id == id);
            if (category is null)
            {
                return false;
            }

            _context.PostCategories.Remove(category);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}