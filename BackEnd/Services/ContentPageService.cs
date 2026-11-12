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
        private readonly IRevisionService _revisions;

        public ContentPageService(AppDbContext context, IHtmlContentSanitizer htmlSanitizer, IRevisionService revisions)
        {
            _context = context;
            _htmlSanitizer = htmlSanitizer;
            _revisions = revisions;
        }

        private void Record(ContentPage page, string action, string? userId)
        {
            _revisions.Add(
                RevisionTypes.Page,
                page.Id,
                page.Title,
                page.Slug,
                page.Excerpt,
                page.Content,
                page.SeoTitle,
                page.MetaDescription,
                action,
                userId);
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
                ScheduledAt = page.ScheduledAt,
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
                    ScheduledAt = p.ScheduledAt,
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

            Record(contentPage, RevisionActions.Create, userId);
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

            var before = (contentPage.Title, contentPage.Slug, contentPage.Content, contentPage.Excerpt, contentPage.SeoTitle, contentPage.MetaDescription);

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

            var after = (contentPage.Title, contentPage.Slug, contentPage.Content, contentPage.Excerpt, contentPage.SeoTitle, contentPage.MetaDescription);

            if (before != after)
            {
                Record(contentPage, RevisionActions.Update, userId);
            }

            await _context.SaveChangesAsync();

            if (before != after)
            {
                await _revisions.PruneAsync(RevisionTypes.Page, contentPage.Id);
            }

            return Map(contentPage);
        }

        public async Task<ContentPageDto?> RestoreRevisionAsync(int id, int revisionId, string? userId, string? rowVersion)
        {
            var contentPage = await _context.ContentPages.FirstOrDefaultAsync(p => p.Id == id);

            if (contentPage is null)
            {
                return null;
            }

            var revision = await _revisions.GetAsync(RevisionTypes.Page, id, revisionId);

            if (revision is null)
            {
                return null;
            }

            RowVersionHelper.SetOriginalValue(_context, contentPage, rowVersion);

            contentPage.Title = revision.Title;
            contentPage.Content = _htmlSanitizer.Sanitize(revision.Content);
            contentPage.Excerpt = revision.Excerpt;
            contentPage.SeoTitle = revision.SeoTitle;
            contentPage.MetaDescription = revision.MetaDescription;
            contentPage.UpdatedByUserId = userId;
            contentPage.UpdatedAt = DateTime.UtcNow;

            Record(contentPage, RevisionActions.Restore, userId);

            await _context.SaveChangesAsync();
            await _revisions.PruneAsync(RevisionTypes.Page, id);

            return Map(contentPage);
        }

        public async Task<ContentPageDto?> SetPublicationAsync(int id, bool isPublished, string? userId, string? rowVersion)
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
            contentPage.ScheduledAt = null;
            contentPage.PublishedAt = isPublished ? contentPage.PublishedAt ?? DateTime.UtcNow : null;
            contentPage.UpdatedByUserId = userId;
            contentPage.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Map(contentPage);
        }

        public async Task<ContentPageDto?> SetScheduleAsync(int id, DateTimeOffset? scheduledAt, string? userId, string? rowVersion)
        {
            var contentPage = await _context.ContentPages.FirstOrDefaultAsync(p => p.Id == id);

            if (contentPage is null)
            {
                return null;
            }

            RowVersionHelper.SetOriginalValue(_context, contentPage, rowVersion);

            if (scheduledAt.HasValue)
            {
                if (contentPage.IsPublished)
                {
                    throw new ArgumentException("این صفحه منتشر شده است، برای زمان‌بندی ابتدا انتشار آن را لغو کنید");
                }

                if (string.IsNullOrWhiteSpace(contentPage.Content))
                {
                    throw new ArgumentException("برای زمان‌بندی انتشار صفحه ابتدا باید محتوای آن را وارد کنید");
                }

                var scheduledUtc = scheduledAt.Value.UtcDateTime;

                if (scheduledUtc <= DateTime.UtcNow)
                {
                    throw new ArgumentException("زمان انتشار باید در آینده باشد");
                }

                contentPage.ScheduledAt = scheduledUtc;
            }
            else
            {
                contentPage.ScheduledAt = null;
            }

            contentPage.UpdatedByUserId = userId;
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

            await _revisions.DeleteAllAsync(RevisionTypes.Page, id);

            return true;
        }
    }
}