using BackEnd.Data;
using BackEnd.DTOs.Common;
using BackEnd.DTOs.Posts;
using BackEnd.Helpers;
using BackEnd.IServices;
using BackEnd.Models;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Services
{
    public class PostService : IPostService
    {
        private readonly AppDbContext _context;
        private readonly IFileService _fileService;
        private readonly IHtmlContentSanitizer _htmlSanitizer;

        public PostService(
            AppDbContext context,
            IFileService fileService,
            IHtmlContentSanitizer htmlSanitizer)
        {
            _context = context;
            _fileService = fileService;
            _htmlSanitizer = htmlSanitizer;
        }

        private static string? Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private PostDto Map(Post post)
        {
            return new PostDto
            {
                Id = post.Id,
                Title = post.Title,
                Slug = post.Slug,
                Excerpt = post.Excerpt,
                Content = _htmlSanitizer.Sanitize(post.Content),
                FeaturedImagePath = string.IsNullOrWhiteSpace(post.FeaturedImagePath)
                    ? null
                    : $"/api/Posts/{post.Id}/featured-image",
                SeoTitle = post.SeoTitle,
                MetaDescription = post.MetaDescription,
                IsPublished = post.IsPublished,
                PublishedAt = post.PublishedAt,
                ScheduledAt = post.ScheduledAt,
                CategoryId = post.CategoryId,
                CategoryName = post.Category?.Name,
                Tags = post.Tags.Select(tag => new TagSummaryDto
                {
                    Id = tag.Id,
                    Name = tag.Name,
                    Slug = tag.Slug
                }).ToList(),
                AuthorId = post.AuthorId,
                UpdatedByUserId = post.UpdatedByUserId,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                RowVersion = RowVersionHelper.Encode(post.RowVersion)
            };
        }

        private async Task<PostCategory?> GetValidCategoryAsync(int? categoryId)
        {
            if (!categoryId.HasValue)
            {
                return null;
            }

            var category = await _context.PostCategories
                .FirstOrDefaultAsync(c => c.Id == categoryId.Value);

            if (category is null)
            {
                throw new ArgumentException("دسته‌بندی انتخاب‌شده وجود ندارد");
            }

            if (!category.IsActive)
            {
                throw new ArgumentException("دسته‌بندی انتخاب‌شده غیرفعال است");
            }

            return category;
        }

        private async Task<List<Tag>> GetValidTagsAsync(List<int> tagIds)
        {
            if (tagIds.Count > 50)
            {
                throw new ArgumentException("هر مقاله حداکثر می‌تواند ۵۰ تگ داشته باشد");
            }

            var uniqueIds = tagIds.Distinct().ToList();

            if (uniqueIds.Count != tagIds.Count)
            {
                throw new ArgumentException("یک تگ را نمی‌توان چند بار انتخاب کرد");
            }

            if (uniqueIds.Count == 0)
            {
                return [];
            }

            var tags = await _context.Tags
                .Where(t => uniqueIds.Contains(t.Id))
                .ToListAsync();

            if (tags.Count != uniqueIds.Count)
            {
                throw new ArgumentException("یک یا چند تگ انتخاب‌شده وجود ندارند");
            }

            return tags;
        }

        public async Task<PagedResult<PostListItemDto>> GetAllAsync(PostQueryDto query)
        {
            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);

            var postsQuery = _context.Posts.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();

                postsQuery = postsQuery.Where(p =>
                    p.Title.Contains(search) ||
                    p.Slug.Contains(search) ||
                    (p.Excerpt != null && p.Excerpt.Contains(search)));
            }

            if (query.IsPublished.HasValue)
            {
                postsQuery = postsQuery.Where(p =>
                    p.IsPublished == query.IsPublished.Value);
            }

            if (query.CategoryId.HasValue)
            {
                postsQuery = postsQuery.Where(p =>
                    p.CategoryId == query.CategoryId.Value);
            }

            var totalCount = await postsQuery.CountAsync();

            var items = await postsQuery
                .OrderByDescending(p => p.CreatedAt)
                .ThenByDescending(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PostListItemDto
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
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    AuthorId = p.AuthorId
                })
                .ToListAsync();

            return new PagedResult<PostListItemDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<PostDto?> GetByIdAsync(int id)
        {
            var post = await _context.Posts
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == id);

            return post is null ? null : Map(post);
        }

        public async Task<PostDto> CreateAsync(CreatePostDto dto, string? userId)
        {
            var title = dto.Title.Trim();

            if (title.Length == 0)
            {
                throw new ArgumentException("عنوان مقاله الزامی است");
            }

            var category = await GetValidCategoryAsync(dto.CategoryId);
            var tags = await GetValidTagsAsync(dto.TagIds);

            var post = new Post
            {
                Title = title,
                Slug = await SlugHelper.ResolveAsync(
                    dto.Slug,
                    title,
                    slug => _context.Posts.AnyAsync(p => p.Slug == slug)),
                Excerpt = Clean(dto.Excerpt),
                Content = _htmlSanitizer.Sanitize(dto.Content),
                SeoTitle = Clean(dto.SeoTitle),
                MetaDescription = Clean(dto.MetaDescription),
                Category = category,
                Tags = tags,
                AuthorId = userId,
                IsPublished = false,
                PublishedAt = null,
                CreatedAt = DateTime.UtcNow
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            return Map(post);
        }

        public async Task<PostDto?> PatchAsync(
            int id,
            PatchPostDto dto,
            string? userId)
        {
            var post = await _context.Posts
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post is null)
            {
                return null;
            }

            RowVersionHelper.SetOriginalValue(
                _context,
                post,
                dto.RowVersion);

            if (dto.CategoryId.HasValue && dto.ClearCategory)
            {
                throw new ArgumentException(
                    "انتخاب دسته‌بندی و حذف دسته‌بندی نمی‌توانند هم‌زمان انجام شوند");
            }

            if (dto.Title is not null)
            {
                var title = dto.Title.Trim();

                if (title.Length == 0)
                {
                    throw new ArgumentException(
                        "عنوان مقاله نمی‌تواند خالی باشد");
                }

                post.Title = title;
            }

            if (dto.Slug is not null &&
                SlugHelper.Generate(dto.Slug) != post.Slug)
            {
                post.Slug = await SlugHelper.ResolveAsync(
                    dto.Slug,
                    post.Title,
                    slug => _context.Posts.AnyAsync(
                        p => p.Slug == slug && p.Id != id));
            }

            if (dto.Excerpt is not null)
            {
                post.Excerpt = Clean(dto.Excerpt);
            }

            if (dto.Content is not null)
            {
                post.Content = _htmlSanitizer.Sanitize(dto.Content);
            }

            if (dto.SeoTitle is not null)
            {
                post.SeoTitle = Clean(dto.SeoTitle);
            }

            if (dto.MetaDescription is not null)
            {
                post.MetaDescription = Clean(dto.MetaDescription);
            }

            if (dto.ClearCategory)
            {
                post.Category = null;
            }
            else if (dto.CategoryId.HasValue)
            {
                post.Category = await GetValidCategoryAsync(dto.CategoryId);
            }

            if (dto.TagIds is not null)
            {
                post.Tags = await GetValidTagsAsync(dto.TagIds);
            }

            post.UpdatedByUserId = userId;
            post.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Map(post);
        }

        public async Task<PostDto?> SetPublicationAsync(
            int id,
            bool isPublished,
            string? userId,
            string? rowVersion)
        {
            var post = await _context.Posts
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post is null)
            {
                return null;
            }

            RowVersionHelper.SetOriginalValue(
                _context,
                post,
                rowVersion);

            if (isPublished &&
                string.IsNullOrWhiteSpace(post.Content))
            {
                throw new ArgumentException(
                    "برای انتشار مقاله ابتدا باید محتوای آن را وارد کنید");
            }

            post.IsPublished = isPublished;
            post.ScheduledAt = null;
            post.PublishedAt = isPublished
                ? post.PublishedAt ?? DateTime.UtcNow
                : null;

            post.UpdatedByUserId = userId;
            post.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Map(post);
        }

        public async Task<PostDto?> SetScheduleAsync(
            int id,
            DateTimeOffset? scheduledAt,
            string? userId,
            string? rowVersion)
        {
            var post = await _context.Posts
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post is null)
            {
                return null;
            }

            RowVersionHelper.SetOriginalValue(
                _context,
                post,
                rowVersion);

            if (scheduledAt.HasValue)
            {
                if (post.IsPublished)
                {
                    throw new ArgumentException(
                        "این مقاله منتشر شده است، برای زمان‌بندی ابتدا انتشار آن را لغو کنید");
                }

                if (string.IsNullOrWhiteSpace(post.Content))
                {
                    throw new ArgumentException(
                        "برای زمان‌بندی انتشار مقاله ابتدا باید محتوای آن را وارد کنید");
                }

                var scheduledUtc = scheduledAt.Value.UtcDateTime;

                if (scheduledUtc <= DateTime.UtcNow)
                {
                    throw new ArgumentException("زمان انتشار باید در آینده باشد");
                }

                post.ScheduledAt = scheduledUtc;
            }
            else
            {
                post.ScheduledAt = null;
            }

            post.UpdatedByUserId = userId;
            post.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Map(post);
        }

        public async Task<bool> SaveFeaturedImageAsync(
            int id,
            IFormFile image,
            string? userId,
            string? rowVersion)
        {
            var post = await _context.Posts
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post is null)
            {
                return false;
            }

            RowVersionHelper.SetOriginalValue(
                _context,
                post,
                rowVersion);

            var oldImagePath = post.FeaturedImagePath;
            var newImagePath = await _fileService.SavePostFeaturedImageAsync(image);

            post.FeaturedImagePath = newImagePath;
            post.UpdatedByUserId = userId;
            post.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                await _fileService.DeletePostFeaturedImageAsync(newImagePath);
                throw;
            }

            if (!string.IsNullOrWhiteSpace(oldImagePath))
            {
                await _fileService.DeletePostFeaturedImageAsync(oldImagePath);
            }

            return true;
        }

        public async Task<bool> DeleteFeaturedImageAsync(
            int id,
            string? userId,
            string? rowVersion)
        {
            var post = await _context.Posts
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post is null)
            {
                return false;
            }

            RowVersionHelper.SetOriginalValue(
                _context,
                post,
                rowVersion);

            var oldImagePath = post.FeaturedImagePath;

            if (string.IsNullOrWhiteSpace(oldImagePath))
            {
                return true;
            }

            post.FeaturedImagePath = null;
            post.UpdatedByUserId = userId;
            post.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _fileService.DeletePostFeaturedImageAsync(oldImagePath);

            return true;
        }

        public async Task<(Stream Stream, string ContentType)?> GetFeaturedImageAsync(int id, bool thumbnail = false)
        {
            var imagePath = await _context.Posts
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => p.FeaturedImagePath)
                .FirstOrDefaultAsync();

            return await _fileService.OpenPostFeaturedImageAsync(imagePath, thumbnail);
        }

        public async Task<bool> DeleteAsync(int id, string? rowVersion)
        {
            var post = await _context.Posts
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post is null)
            {
                return false;
            }

            RowVersionHelper.SetOriginalValue(
                _context,
                post,
                rowVersion);

            var imagePath = post.FeaturedImagePath;

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(imagePath))
            {
                await _fileService.DeletePostFeaturedImageAsync(imagePath);
            }

            return true;
        }
    }
}