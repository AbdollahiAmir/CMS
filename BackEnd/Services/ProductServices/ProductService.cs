using BackEnd.Data;
using BackEnd.DTOs.ProductDTOs;
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

        public ProductService(AppDbContext context, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        public async Task<List<Product>> GetAllAsync(string userId, bool isAdmin)
        {
            var query = _context.Products.AsQueryable();

            if (!isAdmin)
            {
                query = query.Where(p => p.SellerId == userId);
            }

            return await query.Select(p => new Product
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                ImgPath = p.ImgPath,
                SellerId = p.SellerId,
                IsApproved = p.IsApproved,
                CategoryId = p.CategoryId,
                Category = p.Category == null ? null : new ProductCategory { Id = p.Category.Id, Name = p.Category.Name, },
                SellerUserName = isAdmin && (p.SellerUserName != null) ? p.SellerUserName : null
            }).ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(int id, string userId, bool isAdmin)
        {
            var query = _context.Products.Include(p => p.Category).Where(p => p.Id == id);
            if (!isAdmin)
            {
                query = query.Where(p => p.SellerId == userId);
            }

            return await query.FirstOrDefaultAsync();
        }

        public async Task<Product> CreateAsync(CreateProductDto dto, string userId, bool isAdmin)
        {
            var categoryExists = await _context.ProductCategories.AnyAsync(c => c.Id == dto.CategoryId);
            if(!categoryExists)
            {
                throw new ArgumentException("دسته بندی وجود ندارد");
            }

            var product = new Product
            {
                Name = dto.Name,
                Price = dto.Price,
                CategoryId = dto.CategoryId,
                SellerId = userId,
                IsApproved = isAdmin
            };

            if(dto.ImgFile is not null && dto.ImgFile.Length > 0)
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

            await _context.Entry(product).Reference(p => p.Category).LoadAsync();

            return product;
        }

        public async Task<Product?> PatchAsync(int id, PatchProductDto dto, string userId, bool isAdmin)
        {
            var product = await GetByIdAsync(id, userId, isAdmin);
            if(product == null)
            {
                return null;
            }

            if(dto.CategoryId.HasValue)
            {
                var categoryExists = await _context.ProductCategories.AnyAsync(c => c.Id == dto.CategoryId.Value);
                if (!categoryExists)
                {
                    throw new ArgumentException("دسته بندی وجود ندارد");
                }

                product.CategoryId = dto.CategoryId.Value;
            }

            if(dto.Name is not null)
            {
                product.Name = dto.Name;
            }

            if(dto.Price.HasValue)
            {
                product.Price = dto.Price.Value;
            }

            var oldImagePath = product.ImgPath;
            string? newImagePath = null;

            if (dto.ImgFile is not null && dto.ImgFile.Length > 0)
            {
                newImagePath = await _fileService.SaveProductImageAsync(dto.ImgFile);

                product.ImgPath = newImagePath;
            }

            try
            {
                if(!isAdmin)
                {
                    product.IsApproved = false;
                }

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

            return product;
        }

        public async Task<bool> DeleteAsync(int id, string userId, bool isAdmin)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && (isAdmin || p.SellerId == userId));
            if (product is null)
            {
                return false;
            }

            var imagePath = product.ImgPath;

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var now = DateTime.UtcNow;

            var cartItems = await _context.CartItems.Where(i => i.ProductId == id).ToListAsync();

            _context.CartItems.RemoveRange(cartItems);
            _context.Products.RemoveRange(product);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            await _fileService.DeleteProductImageAsync(imagePath);

            return true;
        }

        public async Task<Product?> SetApprovalAsync(int id, bool isApproved)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var product = await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);

            if(product == null)
            {
                return null;
            }

            if(isApproved)
            {
                if (product.CategoryId is null || product.Category is null)
                {
                    throw new ArgumentException("برای تأیید محصول، ابتدا باید دسته‌بندی آن را انتخاب کنید.");
                }

                if (string.IsNullOrWhiteSpace(product.SellerId))
                {
                    throw new ArgumentException("محصول فروشنده ندارد و قابل تأیید نیست.");
                }
            }

            product.IsApproved = isApproved;

            if (!isApproved)
            {
                var cartItems = await _context.CartItems.Where(i => i.ProductId == id).ToListAsync();

                _context.CartItems.RemoveRange(cartItems);

                var now = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return product;
        }

        public async Task<(Stream Stream, string ContentType)?> GetProductImageAsync(int id, string userId, bool isAdmin)
        {
            var product = await GetByIdAsync(id, userId, isAdmin);
            if(product is null)
            {
                return null;
            }

            return await _fileService.OpenProductImageAsync(product.ImgPath);
        }
    }
}