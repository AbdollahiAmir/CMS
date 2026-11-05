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

        public PublicProductService(AppDbContext context, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        public async Task<PagedResult<PublicProductDto>> GetProductsAsync(ProductQueryDto query)
        {
            var categoryIds = await ProductQueryHelper.ResolveCategoryIdsAsync(_context, query.CategoryId, true);

            var products = ProductQueryHelper.ApplyFilters(_context.Products.AsNoTracking().Where(p => p.IsActive), query, categoryIds);

            return await ProductQueryHelper.ToPagedAsync(products.OrderByDescending(p => p.CreatedAt), query, ProductQueryHelper.PublicProjection);
        }

        public async Task<PublicProductDto?> GetBySlugAsync(string slug)
        {
            return await _context.Products.AsNoTracking()
                .Where(p => p.IsActive && p.Slug == slug)
                .Select(ProductQueryHelper.PublicProjection)
                .FirstOrDefaultAsync();
        }

        public async Task<(Stream Stream, string ContentType)?> GetProductImageAsync(int id)
        {
            var imgPath = await _context.Products.AsNoTracking()
                .Where(p => p.Id == id && p.IsActive)
                .Select(p => p.ImgPath)
                .FirstOrDefaultAsync();

            return await _fileService.OpenProductImageAsync(imgPath);
        }
    }
}