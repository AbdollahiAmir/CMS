using BackEnd.Data;
using BackEnd.DTOs.ProductDTOs;
using BackEnd.IServices;
using BackEnd.IServices.IProductServices;
using BackEnd.Models;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Services.ProductServices
{
    public class ProductGalleryService : IProductGalleryService
    {
        private const int MaxImagesPerProduct = 10;

        private readonly AppDbContext _context;
        private readonly IFileService _fileService;

        public ProductGalleryService(AppDbContext context, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        private static ProductImageDto Map(ProductImage image, bool publicUrls)
        {
            return new ProductImageDto
            {
                Id = image.Id,
                AltText = image.AltText,
                SortOrder = image.SortOrder,
                Url = publicUrls
                    ? $"/api/PublicProducts/products/{image.ProductId}/images/{image.Id}"
                    : $"/api/Products/{image.ProductId}/images/{image.Id}/file"
            };
        }

        private static string? Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        public async Task<List<ProductImageDto>> GetImagesAsync(int productId, bool publicUrls)
        {
            var images = await _context.ProductImages
                .AsNoTracking()
                .Where(i => i.ProductId == productId)
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.Id)
                .ToListAsync();

            return images.Select(i => Map(i, publicUrls)).ToList();
        }

        public async Task<ProductImageDto?> AddAsync(int productId, IFormFile image, string? altText)
        {
            if (!await _context.Products.AnyAsync(p => p.Id == productId))
            {
                return null;
            }

            var existing = await _context.ProductImages
                .Where(i => i.ProductId == productId)
                .Select(i => i.SortOrder)
                .ToListAsync();

            if (existing.Count >= MaxImagesPerProduct)
            {
                throw new ArgumentException($"هر محصول حداکثر {MaxImagesPerProduct} تصویر در گالری می‌تواند داشته باشد");
            }

            var path = await _fileService.SaveProductImageAsync(image);

            var entity = new ProductImage
            {
                ProductId = productId,
                Path = path,
                AltText = Clean(altText),
                SortOrder = existing.Count == 0 ? 0 : existing.Max() + 1,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProductImages.Add(entity);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                await _fileService.DeleteProductImageAsync(path);
                throw;
            }

            return Map(entity, false);
        }

        public async Task<ProductImageDto?> UpdateAsync(int productId, int imageId, string? altText)
        {
            var entity = await _context.ProductImages
                .FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId);

            if (entity is null)
            {
                return null;
            }

            entity.AltText = Clean(altText);

            await _context.SaveChangesAsync();

            return Map(entity, false);
        }

        public async Task<List<ProductImageDto>?> ReorderAsync(int productId, IReadOnlyList<int> imageIds)
        {
            if (!await _context.Products.AnyAsync(p => p.Id == productId))
            {
                return null;
            }

            var images = await _context.ProductImages
                .Where(i => i.ProductId == productId)
                .ToListAsync();

            if (imageIds.Count != images.Count
                || imageIds.Distinct().Count() != imageIds.Count
                || imageIds.Any(id => images.All(i => i.Id != id)))
            {
                throw new ArgumentException("فهرست تصاویر باید دقیقا شامل همه‌ی تصاویر این محصول باشد");
            }

            for (var index = 0; index < imageIds.Count; index++)
            {
                images.First(i => i.Id == imageIds[index]).SortOrder = index;
            }

            await _context.SaveChangesAsync();

            return images.OrderBy(i => i.SortOrder).Select(i => Map(i, false)).ToList();
        }

        public async Task<bool> DeleteAsync(int productId, int imageId)
        {
            var entity = await _context.ProductImages
                .FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId);

            if (entity is null)
            {
                return false;
            }

            var path = entity.Path;

            _context.ProductImages.Remove(entity);
            await _context.SaveChangesAsync();

            await _fileService.DeleteProductImageAsync(path);

            return true;
        }

        public async Task<(Stream Stream, string ContentType)?> OpenAsync(int productId, int imageId, bool thumbnail, bool onlyActive)
        {
            var query = _context.ProductImages
                .AsNoTracking()
                .Where(i => i.Id == imageId && i.ProductId == productId);

            if (onlyActive)
            {
                query = query.Where(i => i.Product.IsActive);
            }

            var path = await query.Select(i => i.Path).FirstOrDefaultAsync();

            return await _fileService.OpenProductImageAsync(path, thumbnail);
        }
    }
}
