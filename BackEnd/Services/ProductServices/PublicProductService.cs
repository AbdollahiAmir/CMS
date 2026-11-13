using BackEnd.Data;
using BackEnd.DTOs.Common;
using BackEnd.DTOs.ProductDTOs;
using BackEnd.Helpers;
using BackEnd.IServices;
using BackEnd.IServices.IProductServices;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Services.ProductServices
{
    public class PublicProductService : IPublicProductService
    {
        private readonly AppDbContext _context;
        private readonly IFileService _fileService;
        private readonly IProductGalleryService _gallery;
        private readonly IProductAttributeService _attributes;

        public PublicProductService(
            AppDbContext context,
            IFileService fileService,
            IProductGalleryService gallery,
            IProductAttributeService attributes)
        {
            _context = context;
            _fileService = fileService;
            _gallery = gallery;
            _attributes = attributes;
        }

        public async Task<PagedResult<PublicProductDto>> GetProductsAsync(ProductQueryDto query)
        {
            var categoryIds = await ProductQueryHelper.ResolveCategoryIdsAsync(_context, query.CategoryId, true);

            var products = ProductQueryHelper.ApplyFilters(_context.Products.AsNoTracking().Where(p => p.IsActive), query, categoryIds);

            return await ProductQueryHelper.ToPagedAsync(products.OrderByDescending(p => p.CreatedAt), query, ProductQueryHelper.PublicProjection);
        }

        public async Task<PublicProductDto?> GetBySlugAsync(string slug)
        {
            var product = await _context.Products.AsNoTracking()
                .Where(p => p.IsActive && p.Slug == slug)
                .Select(ProductQueryHelper.PublicProjection)
                .FirstOrDefaultAsync();

            if (product is null)
            {
                return null;
            }

            product.Images = await _gallery.GetImagesAsync(product.Id, true);
            product.Attributes = await _attributes.GetValuesAsync(product.Id);

            return product;
        }

        public async Task<List<FilterAttributeDto>> GetFilterAttributesAsync()
        {
            return await _attributes.GetFilterableAsync();
        }

        public async Task<(Stream Stream, string ContentType)?> GetGalleryImageAsync(int productId, int imageId, bool thumbnail = false)
        {
            return await _gallery.OpenAsync(productId, imageId, thumbnail, true);
        }

        public async Task<List<CartLookupItemDto>> LookupForCartAsync(IReadOnlyCollection<int> productIds)
        {
            var ids = productIds.Distinct().Take(50).ToList();

            return await _context.Products.AsNoTracking()
                .Where(p => p.IsActive && ids.Contains(p.Id))
                .Select(p => new CartLookupItemDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Slug = p.Slug,
                    Price = p.Price,
                    SalePrice = p.SalePrice,
                    UnitPrice = p.SalePrice ?? p.Price,
                    AvailableQuantity = p.StockQuantity > 100 ? 100 : p.StockQuantity,
                    IsAvailable = p.StockQuantity > 0,
                    HasImage = p.ImgPath != null
                })
                .ToListAsync();
        }

        public async Task<(Stream Stream, string ContentType)?> GetProductImageAsync(int id, bool thumbnail = false)
        {
            var imgPath = await _context.Products.AsNoTracking()
                .Where(p => p.Id == id && p.IsActive)
                .Select(p => p.ImgPath)
                .FirstOrDefaultAsync();

            return await _fileService.OpenProductImageAsync(imgPath, thumbnail);
        }
    }
}