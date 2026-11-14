using System.Text.RegularExpressions;
using BackEnd.Data;
using BackEnd.DTOs.Comments;
using BackEnd.DTOs.Common;
using BackEnd.IServices;
using BackEnd.Models;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Services
{
    public partial class CommentService : ICommentService
    {
        private const int MaxPendingPerIp = 10;

        private readonly AppDbContext _context;

        public CommentService(AppDbContext context)
        {
            _context = context;
        }

        [GeneratedRegex(@"[\u0000-\u0008\u000B\u000C\u000E-\u001F‎‏‪-‮]")]
        private static partial Regex ControlCharacters();

        private static string Clean(string value)
        {
            return ControlCharacters().Replace(value, string.Empty).Trim();
        }

        private static AdminCommentDto MapAdmin(PostComment comment, string title, string slug)
        {
            return new AdminCommentDto
            {
                Id = comment.Id,
                PostId = comment.PostId,
                PostTitle = title,
                PostSlug = slug,
                AuthorName = comment.AuthorName,
                AuthorEmail = comment.AuthorEmail,
                Content = comment.Content,
                UserId = comment.UserId,
                Status = comment.Status,
                IpAddress = comment.IpAddress,
                CreatedAt = comment.CreatedAt,
                ModeratedAt = comment.ModeratedAt,
                ModeratedByUserId = comment.ModeratedByUserId
            };
        }

        public async Task<PagedResult<PublicCommentDto>?> GetApprovedAsync(string postSlug, CommentQueryDto query)
        {
            var slug = postSlug.Trim();

            var postId = await _context.Posts
                .AsNoTracking()
                .Where(p => p.Slug == slug && p.IsPublished)
                .Select(p => (int?)p.Id)
                .FirstOrDefaultAsync();

            if (postId is null)
            {
                return null;
            }

            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 50);

            var comments = _context.PostComments
                .AsNoTracking()
                .Where(c => c.PostId == postId && c.Status == CommentStatus.Approved);

            var total = await comments.CountAsync();

            var items = await comments
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new PublicCommentDto
                {
                    Id = c.Id,
                    AuthorName = c.AuthorName,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            return new PagedResult<PublicCommentDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            };
        }

        public async Task<bool?> SubmitAsync(string postSlug, CreateCommentDto dto, string? userId, string? userName, string? ipAddress)
        {
            var slug = postSlug.Trim();

            var postId = await _context.Posts
                .AsNoTracking()
                .Where(p => p.Slug == slug && p.IsPublished)
                .Select(p => (int?)p.Id)
                .FirstOrDefaultAsync();

            if (postId is null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(dto.Website))
            {
                return true;
            }

            var content = Clean(dto.Content);

            if (content.Length < 2)
            {
                throw new ArgumentException("متن نظر خیلی کوتاه است");
            }

            var name = Clean(dto.AuthorName ?? string.Empty);

            if (name.Length == 0)
            {
                name = userName ?? string.Empty;
            }

            if (name.Length == 0)
            {
                throw new ArgumentException("نام الزامی است");
            }

            if (name.Length > 100)
            {
                name = name[..100];
            }

            if (ipAddress is not null)
            {
                var pending = await _context.PostComments.CountAsync(c =>
                    c.IpAddress == ipAddress && c.Status == CommentStatus.Pending);

                if (pending >= MaxPendingPerIp)
                {
                    throw new ArgumentException("تعداد نظرهای در انتظار تایید شما زیاد است، بعدا دوباره تلاش کنید");
                }
            }

            var duplicate = await _context.PostComments.AnyAsync(c =>
                c.PostId == postId && c.Content == content && c.AuthorName == name);

            if (duplicate)
            {
                throw new ArgumentException("این نظر قبلا ثبت شده است");
            }

            _context.PostComments.Add(new PostComment
            {
                PostId = postId.Value,
                AuthorName = name,
                AuthorEmail = string.IsNullOrWhiteSpace(dto.AuthorEmail) ? null : dto.AuthorEmail.Trim(),
                Content = content,
                UserId = userId,
                Status = CommentStatus.Pending,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<PagedResult<AdminCommentDto>> GetAllAsync(AdminCommentQueryDto query)
        {
            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);

            var comments = _context.PostComments.AsNoTracking().AsQueryable();

            if (query.Status.HasValue)
            {
                comments = comments.Where(c => c.Status == query.Status.Value);
            }

            if (query.PostId.HasValue)
            {
                comments = comments.Where(c => c.PostId == query.PostId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();

                comments = comments.Where(c =>
                    c.Content.Contains(search) ||
                    c.AuthorName.Contains(search) ||
                    (c.AuthorEmail != null && c.AuthorEmail.Contains(search)));
            }

            var total = await comments.CountAsync();

            var items = await comments
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new AdminCommentDto
                {
                    Id = c.Id,
                    PostId = c.PostId,
                    PostTitle = c.Post.Title,
                    PostSlug = c.Post.Slug,
                    AuthorName = c.AuthorName,
                    AuthorEmail = c.AuthorEmail,
                    Content = c.Content,
                    UserId = c.UserId,
                    Status = c.Status,
                    IpAddress = c.IpAddress,
                    CreatedAt = c.CreatedAt,
                    ModeratedAt = c.ModeratedAt,
                    ModeratedByUserId = c.ModeratedByUserId
                })
                .ToListAsync();

            return new PagedResult<AdminCommentDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            };
        }

        public async Task<int> GetPendingCountAsync()
        {
            return await _context.PostComments.CountAsync(c => c.Status == CommentStatus.Pending);
        }

        public async Task<AdminCommentDto?> SetStatusAsync(int id, CommentStatus status, string? moderatorId)
        {
            if (!Enum.IsDefined(status))
            {
                throw new ArgumentException("وضعیت نامعتبر است");
            }

            var comment = await _context.PostComments
                .Include(c => c.Post)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comment is null)
            {
                return null;
            }

            comment.Status = status;
            comment.ModeratedAt = DateTime.UtcNow;
            comment.ModeratedByUserId = moderatorId;

            await _context.SaveChangesAsync();

            return MapAdmin(comment, comment.Post.Title, comment.Post.Slug);
        }

        public async Task<int> SetStatusBulkAsync(IReadOnlyList<int> ids, CommentStatus status, string? moderatorId)
        {
            if (!Enum.IsDefined(status))
            {
                throw new ArgumentException("وضعیت نامعتبر است");
            }

            var distinct = ids.Distinct().ToList();
            var now = DateTime.UtcNow;

            return await _context.PostComments
                .Where(c => distinct.Contains(c.Id))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(c => c.Status, status)
                    .SetProperty(c => c.ModeratedAt, now)
                    .SetProperty(c => c.ModeratedByUserId, moderatorId));
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var removed = await _context.PostComments
                .Where(c => c.Id == id)
                .ExecuteDeleteAsync();

            return removed > 0;
        }
    }
}
