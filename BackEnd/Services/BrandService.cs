using BackEnd.Data;
using BackEnd.DTOs.Brands;
using BackEnd.Helpers;
using BackEnd.IServices;
using BackEnd.Models;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Services
{
    public class BrandService : IBrandService
    {
        private readonly AppDbContext _context;
        private readonly IFileService _fileService;

        public BrandService(AppDbContext context, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        private static string? Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        public async Task<List<Brand>> GetAllAsync(bool includeInactive)
        {
            var query = _context.Brands.AsNoTracking();

            if (!includeInactive)
            {
                query = query.Where(b => b.IsActive);
            }

            return await query.OrderBy(b => b.Name).ToListAsync();
        }

        public async Task<Brand?> GetByIdAsync(int id)
        {
            return await _context.Brands.FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<Brand> CreateAsync(CreateBrandDto dto)
        {
            var name = dto.Name.Trim();
            if (name.Length == 0)
            {
                throw new ArgumentException("نام برند الزامی است");
            }

            var brand = new Brand
            {
                Name = name,
                Slug = await SlugHelper.ResolveAsync(dto.Slug, name, s => _context.Brands.AnyAsync(b => b.Slug == s)),
                Description = Clean(dto.Description),
                IsActive = dto.IsActive
            };

            if (dto.LogoFile is { Length: > 0 })
            {
                brand.LogoPath = await _fileService.SaveBrandLogoAsync(dto.LogoFile);
            }

            _context.Brands.Add(brand);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                await _fileService.DeleteBrandLogoAsync(brand.LogoPath);
                throw;
            }

            return brand;
        }

        public async Task<Brand?> PatchAsync(int id, PatchBrandDto dto)
        {
            var brand = await GetByIdAsync(id);
            if (brand is null)
            {
                return null;
            }

            if (dto.Name is not null)
            {
                var name = dto.Name.Trim();
                if (name.Length == 0)
                {
                    throw new ArgumentException("نام برند نمیتواند خالی باشد");
                }

                brand.Name = name;
            }

            if (dto.Slug is not null && SlugHelper.Generate(dto.Slug) != brand.Slug)
            {
                brand.Slug = await SlugHelper.ResolveAsync(dto.Slug, brand.Name, s => _context.Brands.AnyAsync(b => b.Slug == s && b.Id != id));
            }

            if (dto.Description is not null)
            {
                brand.Description = Clean(dto.Description);
            }

            if (dto.IsActive.HasValue)
            {
                brand.IsActive = dto.IsActive.Value;
            }

            var oldLogoPath = brand.LogoPath;
            string? newLogoPath = null;

            if (dto.LogoFile is { Length: > 0 })
            {
                newLogoPath = await _fileService.SaveBrandLogoAsync(dto.LogoFile);
                brand.LogoPath = newLogoPath;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                if (newLogoPath is not null)
                {
                    await _fileService.DeleteBrandLogoAsync(newLogoPath);
                }

                throw;
            }

            if (newLogoPath is not null)
            {
                await _fileService.DeleteBrandLogoAsync(oldLogoPath);
            }

            return brand;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var brand = await GetByIdAsync(id);
            if (brand is null)
            {
                return false;
            }

            var logoPath = brand.LogoPath;

            var products = await _context.Products.Where(p => p.BrandId == id).ToListAsync();
            foreach (var product in products)
            {
                product.BrandId = null;
            }

            _context.Brands.Remove(brand);

            await _context.SaveChangesAsync();

            await _fileService.DeleteBrandLogoAsync(logoPath);

            return true;
        }

        public async Task<(Stream Stream, string ContentType)?> GetBrandLogoAsync(int id, bool onlyActive, bool thumbnail = false)
        {
            var brand = await _context.Brands.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id && (!onlyActive || b.IsActive));
            if (brand is null)
            {
                return null;
            }

            return await _fileService.OpenBrandLogoAsync(brand.LogoPath, thumbnail);
        }
    }
}