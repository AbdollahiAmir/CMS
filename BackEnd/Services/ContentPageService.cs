using BackEnd.Data;
using BackEnd.DTOs.Common;
using BackEnd.DTOs.Pages;
using BackEnd.Helpers;
using BackEnd.IServices;
using BackEnd.Models;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Services
{
    public class ContentPageService : IContentPageService
    {
        private readonly AppDbContext _context;
        private readonly IHtmlContentSanitizer _htmlSanitizer;

        public ContentPageService(AppDbContext context, IHtmlContentSanitizer htmlSanitizer)
        {
            _context = context;
            _htmlSanitizer = htmlSanitizer;
        }

        private static string? Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private ContentPageDto Map(ContentPage page)
        {
            return new ContentPageDto
            {
                Id = page.Id,
                Title = page.Title,
                Slug = page.Slug,
                Content = _htmlSanitizer.Sanitize(page.Content),
                Excerpt = page.Excerpt,
                SeoTitle = page.SeoTitle,
                MetaDescription = page.MetaDescription,
                IsPublished = page.IsPublished,
                PublishedAt = page.PublishedAt,
                CreatedByUserId = page.CreatedByUserId,
                UpdatedByUserId = page.UpdatedByUserId,
                CreatedAt = page.CreatedAt,
                UpdatedAt = page.UpdatedAt,
                RowVersion = RowVersionHelper.Encode(page.RowVersion)
            };
        }

        public async Task<PagedResult<PageListItemDto>> GetAllAsync(PageQueryDto query)
        {
            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);

            var pagesQuery = _context.ContentPages.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();

                pagesQuery = pagesQuery.Where(p => p.Title.Contains(search) || p.Slug.Contains(search));
            }

            if (query.IsPublished.HasValue)
            {
                pagesQuery = pagesQuery.Where(p => p.IsPublished == query.IsPublished.Value);
            }

            var totalCount = await pagesQuery.CountAsync();

            var items = await pagesQuery.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id).Skip((page - 1) * pageSize).Take(pageSize).Select(p => new PageListItemDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Slug = p.Slug,
                    Excerpt = p.Excerpt,
                    IsPublished = p.IsPublished,
                    PublishedAt = p.PublishedAt,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    CreatedByUserId = p.CreatedByUserId,
                    UpdatedByUserId = p.UpdatedByUserId
                }).ToListAsync();

            return new PagedResult<PageListItemDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<ContentPageDto?> GetByIdAsync(int id)
        {
            var page = await _context.ContentPages.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);

            return page is null ? null : Map(page);
        }

        public async Task<ContentPageDto> CreateAsync(CreateContentPageDto dto, string? userId)
        {
            var title = dto.Title.Trim();

            if (title.Length == 0)
            {
                throw new ArgumentException("عنوان صفحه الزامی است");
            }

            var contentPage = new ContentPage
            {
                Title = title,
                Slug = await SlugHelper.ResolveAsync(dto.Slug, title, slug => _context.ContentPages.AnyAsync(p => p.Slug == slug)),
                Content = _htmlSanitizer.Sanitize(dto.Content),
                Excerpt = Clean(dto.Excerpt),
                SeoTitle = Clean(dto.SeoTitle),
                MetaDescription = Clean(dto.MetaDescription),
                IsPublished = false,
                PublishedAt = null,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.ContentPages.Add(contentPage);
            await _context.SaveChangesAsync();

            return Map(contentPage);
        }

        public async Task<ContentPageDto?> PatchAsync(int id, PatchContentPageDto dto, string? userId)
        {
            var contentPage = await _context.ContentPages.FirstOrDefaultAsync(p => p.Id == id);

            if (contentPage is null)
            {
                return null;
            }

            RowVersionHelper.SetOriginalValue(_context, contentPage, dto.RowVersion);

            if (dto.Title is not null)
            {
                var title = dto.Title.Trim();

                if (title.Length == 0)
                {
                    throw new ArgumentException("عنوان صفحه نمی‌تواند خالی باشد");
                }

                contentPage.Title = title;
            }

            if (dto.Slug is not null && SlugHelper.Generate(dto.Slug) != contentPage.Slug)
            {
                contentPage.Slug = await SlugHelper.ResolveAsync(dto.Slug, contentPage.Title, slug => _context.ContentPages.AnyAsync(p => p.Slug == slug && p.Id != id));
            }

            if (dto.Content is not null)
            {
                contentPage.Content = _htmlSanitizer.Sanitize(dto.Content);
            }

            if (dto.Excerpt is not null)
            {
                contentPage.Excerpt = Clean(dto.Excerpt);
            }

            if (dto.SeoTitle is not null)
            {
                contentPage.SeoTitle = Clean(dto.SeoTitle);
            }

            if (dto.MetaDescription is not null)
            {
                contentPage.MetaDescription = Clean(dto.MetaDescription);
            }

            contentPage.UpdatedByUserId = userId;
            contentPage.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Map(contentPage);
        }

        public async Task<ContentPageDto?> SetPublicationAsync(int id, bool isPublished, string? rowVersion)
        {
            var contentPage = await _context.ContentPages.FirstOrDefaultAsync(p => p.Id == id);

            if (contentPage is null)
            {
                return null;
            }

            RowVersionHelper.SetOriginalValue(_context, contentPage, rowVersion);

            if (isPublished && string.IsNullOrWhiteSpace(contentPage.Content))
            {
                throw new ArgumentException("برای انتشار صفحه ابتدا باید محتوای آن را وارد کنید");
            }

            contentPage.IsPublished = isPublished;
            contentPage.PublishedAt = isPublished ? contentPage.PublishedAt ?? DateTime.UtcNow : null;
            contentPage.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Map(contentPage);
        }

        public async Task<bool> DeleteAsync(int id, string? rowVersion)
        {
            var contentPage = await _context.ContentPages.FirstOrDefaultAsync(p => p.Id == id);

            if (contentPage is null)
            {
                return false;
            }

            RowVersionHelper.SetOriginalValue(_context, contentPage, rowVersion);

            _context.ContentPages.Remove(contentPage);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}