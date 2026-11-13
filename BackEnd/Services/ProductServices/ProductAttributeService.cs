using BackEnd.Data;
using BackEnd.DTOs.ProductDTOs;
using BackEnd.Helpers;
using BackEnd.IServices.IProductServices;
using BackEnd.Models;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Services.ProductServices
{
    public class ProductAttributeService : IProductAttributeService
    {
        private const int MaxItemsPerProduct = 50;

        private readonly AppDbContext _context;

        public ProductAttributeService(AppDbContext context)
        {
            _context = context;
        }

        private static ProductAttributeDto Map(ProductAttribute attribute)
        {
            return new ProductAttributeDto
            {
                Id = attribute.Id,
                Name = attribute.Name,
                Slug = attribute.Slug,
                SortOrder = attribute.SortOrder,
                IsFilterable = attribute.IsFilterable
            };
        }

        public async Task<List<ProductAttributeDto>> GetAllAsync()
        {
            return await _context.ProductAttributes
                .AsNoTracking()
                .OrderBy(a => a.SortOrder)
                .ThenBy(a => a.Name)
                .Select(a => new ProductAttributeDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Slug = a.Slug,
                    SortOrder = a.SortOrder,
                    IsFilterable = a.IsFilterable
                })
                .ToListAsync();
        }

        public async Task<ProductAttributeDto> CreateAsync(CreateProductAttributeDto dto)
        {
            var name = dto.Name.Trim();

            if (name.Length == 0)
            {
                throw new ArgumentException("نام ویژگی الزامی است");
            }

            var attribute = new ProductAttribute
            {
                Name = name,
                Slug = await SlugHelper.ResolveAsync(
                    dto.Slug,
                    name,
                    slug => _context.ProductAttributes.AnyAsync(a => a.Slug == slug)),
                SortOrder = dto.SortOrder,
                IsFilterable = dto.IsFilterable
            };

            _context.ProductAttributes.Add(attribute);
            await _context.SaveChangesAsync();

            return Map(attribute);
        }

        public async Task<ProductAttributeDto?> PatchAsync(int id, PatchProductAttributeDto dto)
        {
            var attribute = await _context.ProductAttributes.FirstOrDefaultAsync(a => a.Id == id);

            if (attribute is null)
            {
                return null;
            }

            if (dto.Name is not null)
            {
                var name = dto.Name.Trim();

                if (name.Length == 0)
                {
                    throw new ArgumentException("نام ویژگی نمی‌تواند خالی باشد");
                }

                attribute.Name = name;
            }

            if (dto.SortOrder.HasValue)
            {
                attribute.SortOrder = dto.SortOrder.Value;
            }

            if (dto.IsFilterable.HasValue)
            {
                attribute.IsFilterable = dto.IsFilterable.Value;
            }

            await _context.SaveChangesAsync();

            return Map(attribute);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var attribute = await _context.ProductAttributes.FirstOrDefaultAsync(a => a.Id == id);

            if (attribute is null)
            {
                return false;
            }

            _context.ProductAttributes.Remove(attribute);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<ProductAttributeValueDto>> GetValuesAsync(int productId)
        {
            return await _context.ProductAttributeValues
                .AsNoTracking()
                .Where(v => v.ProductId == productId)
                .OrderBy(v => v.Attribute.SortOrder)
                .ThenBy(v => v.Attribute.Name)
                .Select(v => new ProductAttributeValueDto
                {
                    AttributeId = v.AttributeId,
                    Name = v.Attribute.Name,
                    Slug = v.Attribute.Slug,
                    Value = v.Value
                })
                .ToListAsync();
        }

        public async Task<List<ProductAttributeValueDto>?> SetValuesAsync(int productId, IReadOnlyList<ProductAttributeValueInputDto> items)
        {
            if (!await _context.Products.AnyAsync(p => p.Id == productId))
            {
                return null;
            }

            if (items.Count > MaxItemsPerProduct)
            {
                throw new ArgumentException($"حداکثر {MaxItemsPerProduct} ویژگی برای هر محصول مجاز است");
            }

            if (items.Select(i => i.AttributeId).Distinct().Count() != items.Count)
            {
                throw new ArgumentException("هر ویژگی فقط یک‌بار می‌تواند برای محصول ثبت شود");
            }

            var desired = items
                .Select(i => (i.AttributeId, Value: i.Value?.Trim() ?? string.Empty))
                .Where(i => i.Value.Length > 0)
                .ToDictionary(i => i.AttributeId, i => i.Value);

            var attributeIds = desired.Keys.ToList();

            var knownCount = await _context.ProductAttributes.CountAsync(a => attributeIds.Contains(a.Id));

            if (knownCount != attributeIds.Count)
            {
                throw new ArgumentException("ویژگی نامعتبر");
            }

            var existing = await _context.ProductAttributeValues
                .Where(v => v.ProductId == productId)
                .ToListAsync();

            foreach (var value in existing)
            {
                if (desired.TryGetValue(value.AttributeId, out var newValue))
                {
                    value.Value = newValue;
                    desired.Remove(value.AttributeId);
                }
                else
                {
                    _context.ProductAttributeValues.Remove(value);
                }
            }

            foreach (var pair in desired)
            {
                _context.ProductAttributeValues.Add(new ProductAttributeValue
                {
                    ProductId = productId,
                    AttributeId = pair.Key,
                    Value = pair.Value
                });
            }

            await _context.SaveChangesAsync();

            return await GetValuesAsync(productId);
        }

        public async Task<List<FilterAttributeDto>> GetFilterableAsync()
        {
            var attributes = await _context.ProductAttributes
                .AsNoTracking()
                .Where(a => a.IsFilterable)
                .OrderBy(a => a.SortOrder)
                .ThenBy(a => a.Name)
                .ToListAsync();

            var rows = await _context.ProductAttributeValues
                .AsNoTracking()
                .Where(v => v.Attribute.IsFilterable && v.Product.IsActive)
                .Select(v => new { v.AttributeId, v.Value })
                .Distinct()
                .ToListAsync();

            return attributes
                .Select(a => new FilterAttributeDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Slug = a.Slug,
                    Values = rows
                        .Where(r => r.AttributeId == a.Id)
                        .Select(r => r.Value)
                        .OrderBy(v => v, StringComparer.CurrentCulture)
                        .ToList()
                })
                .Where(a => a.Values.Count > 0)
                .ToList();
        }
    }
}
