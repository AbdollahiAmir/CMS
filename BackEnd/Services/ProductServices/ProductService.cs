using BackEnd.Data;
using BackEnd.DTOs.Common;
using BackEnd.DTOs.ProductDTOs;
using BackEnd.Helpers;
using BackEnd.IServices;
using BackEnd.IServices.IProductServices;
using BackEnd.Models;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Services.ProductServices
{
    public class ProductService : IProductService
    {
        private readonly AppDbContext _context;
        private readonly IFileService _fileService;
        private readonly IHtmlContentSanitizer _htmlSanitizer;

        public ProductService(AppDbContext context, IFileService fileService, IHtmlContentSanitizer htmlSanitizer)
        {
            _context = context;
            _fileService = fileService;
            _htmlSanitizer = htmlSanitizer;
        }

        private static string? Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private string? CleanHtml(string? value)
        {
            return Clean(_htmlSanitizer.Sanitize(value));
        }

        private static void ValidatePricing(decimal price, decimal? salePrice)
        {
            if (salePrice.HasValue && salePrice.Value >= price)
            {
                throw new ArgumentException("قیمت با تخفیف باید کمتر از قیمت اصلی باشد");
            }
        }

        private async Task<List<ProductCategory>> LoadCategoriesAsync(IEnumerable<int> categoryIds)
        {
            var ids = categoryIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return new List<ProductCategory>();
            }

            var categories = await _context.ProductCategories.Where(c => ids.Contains(c.Id)).ToListAsync();
            if (categories.Count != ids.Count)
            {
                throw new ArgumentException("دسته بندی نامعتبر");
            }

            return categories;
        }

        private async Task EnsureBrandExistsAsync(int brandId)
        {
            if (!await _context.Brands.AnyAsync(b => b.Id == brandId))
            {
                throw new ArgumentException("برند وجود ندارد");
            }
        }

        public async Task<PagedResult<ProductDto>> GetAllAsync(ProductQueryDto query)
        {
            var categoryIds = await ProductQueryHelper.ResolveCategoryIdsAsync(_context, query.CategoryId, false);

            var products = ProductQueryHelper.ApplyFilters(_context.Products.AsNoTracking(), query, categoryIds);

            if (query.IsActive.HasValue)
            {
                products = products.Where(p => p.IsActive == query.IsActive.Value);
            }

            return await ProductQueryHelper.ToPagedAsync(products.OrderByDescending(p => p.Id), query, ProductQueryHelper.AdminProjection);
        }

        public async Task<ProductDto?> GetByIdAsync(int id)
        {
            return await _context.Products.AsNoTracking().Where(p => p.Id == id).Select(ProductQueryHelper.AdminProjection).FirstOrDefaultAsync();
        }

        public async Task<ProductDto> CreateAsync(CreateProductDto dto)
        {
            var name = dto.Name.Trim();
            if (name.Length == 0)
            {
                throw new ArgumentException("نام محصول الزامی است");
            }

            ValidatePricing(dto.Price, dto.SalePrice);

            var categories = await LoadCategoriesAsync(dto.CategoryIds);

            if (dto.BrandId.HasValue)
            {
                await EnsureBrandExistsAsync(dto.BrandId.Value);
            }

            var sku = Clean(dto.Sku);
            if (sku is not null && await _context.Products.AnyAsync(p => p.Sku == sku))
            {
                throw new ArgumentException("این کد محصول (SKU) قبلا استفاده شده است");
            }

            var product = new Product
            {
                Name = name,
                Slug = await SlugHelper.ResolveAsync(dto.Slug, name, s => _context.Products.AnyAsync(p => p.Slug == s)),
                Sku = sku,
                ShortDescription = Clean(dto.ShortDescription),
                Description = CleanHtml(dto.Description),
                SeoTitle = Clean(dto.SeoTitle),
                MetaDescription = Clean(dto.MetaDescription),
                Price = dto.Price,
                SalePrice = dto.SalePrice,
                StockQuantity = dto.StockQuantity,
                BrandId = dto.BrandId,
                IsActive = false,
                Categories = categories
            };

            if (dto.ImgFile is { Length: > 0 })
            {
                product.ImgPath = await _fileService.SaveProductImageAsync(dto.ImgFile);
            }

            _context.Products.Add(product);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                await _fileService.DeleteProductImageAsync(product.ImgPath);
                throw;
            }

            return (await GetByIdAsync(product.Id))!;
        }

        public async Task<ProductDto?> PatchAsync(int id, PatchProductDto dto)
        {
            var product = await _context.Products.Include(p => p.Categories).FirstOrDefaultAsync(p => p.Id == id);
            if (product is null)
            {
                return null;
            }

            RowVersionHelper.SetOriginalValue(_context, product, dto.RowVersion);

            if (dto.Name is not null)
            {
                var name = dto.Name.Trim();
                if (name.Length == 0)
                {
                    throw new ArgumentException("نام محصول نمیتواند خالی باشد");
                }

                product.Name = name;
            }

            if (dto.Slug is not null && SlugHelper.Generate(dto.Slug) != product.Slug)
            {
                product.Slug = await SlugHelper.ResolveAsync(dto.Slug, product.Name, s => _context.Products.AnyAsync(p => p.Slug == s && p.Id != id));
            }

            if (dto.Sku is not null)
            {
                var sku = Clean(dto.Sku);
                if (sku != product.Sku)
                {
                    if (sku is not null && await _context.Products.AnyAsync(p => p.Sku == sku && p.Id != id))
                    {
                        throw new ArgumentException("این کد محصول (SKU) قبلا استفاده شده است");
                    }

                    product.Sku = sku;
                }
            }

            if (dto.ShortDescription is not null)
            {
                product.ShortDescription = Clean(dto.ShortDescription);
            }

            if (dto.Description is not null)
            {
                product.Description = CleanHtml(dto.Description);
            }

            if (dto.SeoTitle is not null)
            {
                product.SeoTitle = Clean(dto.SeoTitle);
            }

            if (dto.MetaDescription is not null)
            {
                product.MetaDescription = Clean(dto.MetaDescription);
            }

            if (dto.Price.HasValue)
            {
                product.Price = dto.Price.Value;
            }

            if (dto.SalePrice.HasValue)
            {
                product.SalePrice = dto.SalePrice.Value == 0 ? null : dto.SalePrice.Value;
            }

            ValidatePricing(product.Price, product.SalePrice);

            if (dto.StockQuantity.HasValue)
            {
                product.StockQuantity = dto.StockQuantity.Value;
            }

            if (dto.BrandId.HasValue)
            {
                if (dto.BrandId.Value == 0)
                {
                    product.BrandId = null;
                }
                else
                {
                    await EnsureBrandExistsAsync(dto.BrandId.Value);
                    product.BrandId = dto.BrandId.Value;
                }
            }

            if (dto.CategoryIds is not null)
            {
                var categories = await LoadCategoriesAsync(dto.CategoryIds);

                product.Categories.Clear();
                foreach (var category in categories)
                {
                    product.Categories.Add(category);
                }
            }

            if (product.IsActive && product.Categories.Count == 0)
            {
                throw new ArgumentException("محصول فعال باید حداقل یک دسته بندی داشته باشد");
            }

            var oldImagePath = product.ImgPath;
            string? newImagePath = null;

            if (dto.ImgFile is { Length: > 0 })
            {
                newImagePath = await _fileService.SaveProductImageAsync(dto.ImgFile);
                product.ImgPath = newImagePath;
            }

            product.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                if (newImagePath is not null)
                {
                    await _fileService.DeleteProductImageAsync(newImagePath);
                }

                throw;
            }

            if (newImagePath is not null)
            {
                await _fileService.DeleteProductImageAsync(oldImagePath);
            }

            return await GetByIdAsync(id);
        }

        public async Task<bool> DeleteAsync(int id, string? rowVersion)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product is null)
            {
                return false;
            }

            RowVersionHelper.SetOriginalValue(_context, product, rowVersion);

            var imagePath = product.ImgPath;

            var cartItems = await _context.CartItems.Where(i => i.ProductId == id).ToListAsync();

            _context.CartItems.RemoveRange(cartItems);
            _context.Products.Remove(product);

            await _context.SaveChangesAsync();

            await _fileService.DeleteProductImageAsync(imagePath);

            return true;
        }

        public async Task<ProductDto?> SetActiveAsync(int id, bool isActive, string? rowVersion)
        {
            var product = await _context.Products.Include(p => p.Categories).FirstOrDefaultAsync(p => p.Id == id);
            if (product is null)
            {
                return null;
            }

            RowVersionHelper.SetOriginalValue(_context, product, rowVersion);

            if (isActive && product.Categories.Count == 0)
            {
                throw new ArgumentException("برای فعال‌سازی محصول، ابتدا باید حداقل یک دسته بندی برایش انتخاب کنید");
            }

            product.IsActive = isActive;
            product.UpdatedAt = DateTime.UtcNow;

            if (!isActive)
            {
                var cartItems = await _context.CartItems.Where(i => i.ProductId == id).ToListAsync();
                _context.CartItems.RemoveRange(cartItems);
            }

            await _context.SaveChangesAsync();

            return await GetByIdAsync(id);
        }

        public async Task<(Stream Stream, string ContentType)?> GetProductImageAsync(int id, bool thumbnail = false)
        {
            var imgPath = await _context.Products.AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => p.ImgPath)
                .FirstOrDefaultAsync();

            return await _fileService.OpenProductImageAsync(imgPath, thumbnail);
        }
    }
}