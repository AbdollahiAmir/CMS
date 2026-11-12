using BackEnd.Data;
using BackEnd.DTOs.Common;
using BackEnd.DTOs.Media;
using BackEnd.IServices;
using BackEnd.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BackEnd.Services
{
    public class MediaService : IMediaService
    {
        private const string FileUrlPrefix = "/api/Media/files/";
        private const int MaxOriginalNameLength = 255;

        private static readonly Expression<Func<MediaFile, MediaDto>> Projection = m => new MediaDto
        {
            Id = m.Id,
            OriginalName = m.OriginalName,
            AltText = m.AltText,
            Url = FileUrlPrefix + m.StoredName,
            ThumbnailUrl = FileUrlPrefix + m.StoredName + "?size=thumb",
            UploadedByUserId = m.UploadedByUserId,
            CreatedAt = m.CreatedAt
        };

        private readonly AppDbContext _context;
        private readonly IFileService _fileService;

        public MediaService(AppDbContext context, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        private static string? Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string NormalizeOriginalName(string? fileName)
        {
            var name = Path.GetFileName(fileName ?? string.Empty).Trim();

            if (name.Length == 0)
            {
                return "image";
            }

            return name.Length > MaxOriginalNameLength ? name[..MaxOriginalNameLength] : name;
        }

        public async Task<PagedResult<MediaDto>> GetAllAsync(MediaQueryDto query)
        {
            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);

            var mediaQuery = _context.MediaFiles.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();

                mediaQuery = mediaQuery.Where(m =>
                    m.OriginalName.Contains(search) ||
                    (m.AltText != null && m.AltText.Contains(search)));
            }

            var totalCount = await mediaQuery.CountAsync();

            var items = await mediaQuery
                .OrderByDescending(m => m.CreatedAt)
                .ThenByDescending(m => m.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(Projection)
                .ToListAsync();

            return new PagedResult<MediaDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<MediaDto?> GetByIdAsync(int id)
        {
            return await _context.MediaFiles
                .AsNoTracking()
                .Where(m => m.Id == id)
                .Select(Projection)
                .FirstOrDefaultAsync();
        }

        public async Task<MediaDto> UploadAsync(IFormFile image, string? altText, string? userId)
        {
            var storedPath = await _fileService.SaveMediaImageAsync(image);

            var media = new MediaFile
            {
                StoredName = Path.GetFileName(storedPath),
                OriginalName = NormalizeOriginalName(image.FileName),
                AltText = Clean(altText),
                UploadedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.MediaFiles.Add(media);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                await _fileService.DeleteMediaImageAsync(storedPath);
                throw;
            }

            return (await GetByIdAsync(media.Id))!;
        }

        public async Task<MediaDto?> UpdateAsync(int id, UpdateMediaDto dto)
        {
            var media = await _context.MediaFiles.FirstOrDefaultAsync(m => m.Id == id);

            if (media is null)
            {
                return null;
            }

            media.AltText = Clean(dto.AltText);

            await _context.SaveChangesAsync();

            return await GetByIdAsync(id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var media = await _context.MediaFiles.FirstOrDefaultAsync(m => m.Id == id);

            if (media is null)
            {
                return false;
            }

            var url = FileUrlPrefix + media.StoredName;

            var isUsed =
                await _context.Posts.AnyAsync(p => p.Content.Contains(url)) ||
                await _context.ContentPages.AnyAsync(p => p.Content.Contains(url)) ||
                await _context.Products.AnyAsync(p => p.Description != null && p.Description.Contains(url)) ||
                await _context.MenuItems.AnyAsync(i => i.Url.Contains(url)) ||
                await _context.SiteSettings.AnyAsync(s =>
                    (s.LogoUrl != null && s.LogoUrl.Contains(url)) ||
                    (s.FaviconUrl != null && s.FaviconUrl.Contains(url)));

            if (isUsed)
            {
                throw new ArgumentException("این تصویر در محتوای یک مقاله، صفحه، محصول، منو یا تنظیمات سایت استفاده شده و قابل حذف نیست");
            }

            var storedName = media.StoredName;

            _context.MediaFiles.Remove(media);
            await _context.SaveChangesAsync();

            await _fileService.DeleteMediaImageAsync(storedName);

            return true;
        }

        public async Task<(Stream Stream, string ContentType)?> OpenFileAsync(string fileName, bool thumbnail = false)
        {
            var storedName = Path.GetFileName(fileName);

            if (string.IsNullOrWhiteSpace(storedName))
            {
                return null;
            }

            var exists = await _context.MediaFiles
                .AsNoTracking()
                .AnyAsync(m => m.StoredName == storedName);

            if (!exists)
            {
                return null;
            }

            return await _fileService.OpenMediaImageAsync(storedName, thumbnail);
        }
    }
}
