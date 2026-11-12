using BackEnd.Data;
using BackEnd.DTOs.Common;
using BackEnd.DTOs.PublicContent;
using BackEnd.IServices;
using BackEnd.Models;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Services
{
    public class PublicContentService : IPublicContentService
    {
        private readonly AppDbContext _context;
        private readonly IFileService _fileService;
        private readonly IHtmlContentSanitizer _htmlSanitizer;

        public PublicContentService(
            AppDbContext context,
            IFileService fileService,
            IHtmlContentSanitizer htmlSanitizer)
        {
            _context = context;
            _fileService = fileService;
            _htmlSanitizer = htmlSanitizer;
        }

        private static PublicTagDto MapTag(Tag tag)
        {
            return new PublicTagDto
            {
                Name = tag.Name,
                Slug = tag.Slug
            };
        }

        private PublicPageDto MapPage(ContentPage page)
        {
            return new PublicPageDto
            {
                Title = page.Title,
                Slug = page.Slug,
                Content = _htmlSanitizer.Sanitize(page.Content),
                Excerpt = page.Excerpt,
                SeoTitle = page.SeoTitle,
                MetaDescription = page.MetaDescription,
                PublishedAt = page.PublishedAt
            };
        }

        private static PublicPostListItemDto MapPostListItem(Post post)
        {
            var category = post.Category?.IsActive == true
                ? post.Category
                : null;

            return new PublicPostListItemDto
            {
                Title = post.Title,
                Slug = post.Slug,
                Excerpt = post.Excerpt,
                FeaturedImageUrl = string.IsNullOrWhiteSpace(post.FeaturedImagePath)
                    ? null
                    : $"/api/PublicContent/posts/{Uri.EscapeDataString(post.Slug)}/featured-image",
                CategoryName = category?.Name,
                CategorySlug = category?.Slug,
                PublishedAt = post.PublishedAt,
                Tags = post.Tags
                    .Select(MapTag)
                    .ToList()
            };
        }

        private PublicPostDto MapPost(Post post)
        {
            var category = post.Category?.IsActive == true
                ? post.Category
                : null;

            return new PublicPostDto
            {
                Title = post.Title,
                Slug = post.Slug,
                Excerpt = post.Excerpt,
                Content = _htmlSanitizer.Sanitize(post.Content),
                FeaturedImageUrl = string.IsNullOrWhiteSpace(post.FeaturedImagePath)
                    ? null
                    : $"/api/PublicContent/posts/{Uri.EscapeDataString(post.Slug)}/featured-image",
                SeoTitle = post.SeoTitle,
                MetaDescription = post.MetaDescription,
                CategoryName = category?.Name,
                CategorySlug = category?.Slug,
                PublishedAt = post.PublishedAt,
                Tags = post.Tags
                    .Select(MapTag)
                    .ToList()
            };
        }

        public async Task<List<PublicPageListItemDto>> GetPagesAsync()
        {
            return await _context.ContentPages
                .AsNoTracking()
                .Where(p => p.IsPublished)
                .OrderBy(p => p.Title)
                .Select(p => new PublicPageListItemDto
                {
                    Title = p.Title,
                    Slug = p.Slug,
                    Excerpt = p.Excerpt,
                    PublishedAt = p.PublishedAt
                })
                .ToListAsync();
        }

        public async Task<PublicPageDto?> GetPageBySlugAsync(string slug)
        {
            var normalizedSlug = slug.Trim();

            var page = await _context.ContentPages
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.Slug == normalizedSlug &&
                    p.IsPublished);

            return page is null ? null : MapPage(page);
        }

        public async Task<PagedResult<PublicPostListItemDto>> GetPostsAsync(
            PublicPostQueryDto query)
        {
            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 50);

            var postsQuery = _context.Posts
                .AsNoTracking()
                .Where(p => p.IsPublished);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();

                postsQuery = postsQuery.Where(p =>
                    p.Title.Contains(search) ||
                    (p.Excerpt != null && p.Excerpt.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(query.CategorySlug))
            {
                var categorySlug = query.CategorySlug.Trim();

                postsQuery = postsQuery.Where(p =>
                    p.Category != null &&
                    p.Category.IsActive &&
                    p.Category.Slug == categorySlug);
            }

            if (!string.IsNullOrWhiteSpace(query.TagSlug))
            {
                var tagSlug = query.TagSlug.Trim();

                postsQuery = postsQuery.Where(p =>
                    p.Tags.Any(t => t.Slug == tagSlug));
            }

            var totalCount = await postsQuery.CountAsync();

            var posts = await postsQuery
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .OrderByDescending(p => p.PublishedAt)
                .ThenByDescending(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<PublicPostListItemDto>
            {
                Items = posts.Select(MapPostListItem).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<PublicPostDto?> GetPostBySlugAsync(string slug)
        {
            var normalizedSlug = slug.Trim();

            var post = await _context.Posts
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p =>
                    p.Slug == normalizedSlug &&
                    p.IsPublished);

            return post is null ? null : MapPost(post);
        }

        public async Task<List<PublicCategoryDto>> GetCategoriesAsync()
        {
            return await _context.PostCategories
                .AsNoTracking()
                .Where(c =>
                    c.IsActive &&
                    c.Posts.Any(p => p.IsPublished))
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .Select(c => new PublicCategoryDto
                {
                    Name = c.Name,
                    Slug = c.Slug,
                    Description = c.Description
                })
                .ToListAsync();
        }

        public async Task<List<PublicTagDto>> GetTagsAsync()
        {
            return await _context.Tags
                .AsNoTracking()
                .Where(t => t.Posts.Any(p => p.IsPublished))
                .OrderBy(t => t.Name)
                .Select(t => new PublicTagDto
                {
                    Name = t.Name,
                    Slug = t.Slug
                })
                .ToListAsync();
        }

        public async Task<PublicPostDto?> GetPostPreviewAsync(int id)
        {
            var post = await _context.Posts
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post is null)
            {
                return null;
            }

            var dto = MapPost(post);

            dto.FeaturedImageUrl = string.IsNullOrWhiteSpace(post.FeaturedImagePath)
                ? null
                : $"/api/Posts/{post.Id}/featured-image";

            return dto;
        }

        public async Task<PublicPageDto?> GetPagePreviewAsync(int id)
        {
            var page = await _context.ContentPages
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            return page is null ? null : MapPage(page);
        }

        public async Task<(Stream Stream, string ContentType)?> GetPostFeaturedImageAsync(
            string slug,
            bool thumbnail = false)
        {
            var normalizedSlug = slug.Trim();

            var imagePath = await _context.Posts
                .AsNoTracking()
                .Where(p =>
                    p.Slug == normalizedSlug &&
                    p.IsPublished)
                .Select(p => p.FeaturedImagePath)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(imagePath))
            {
                return null;
            }

            return await _fileService.OpenPostFeaturedImageAsync(imagePath, thumbnail);
        }
    }
}