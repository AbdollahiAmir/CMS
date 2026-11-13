using BackEnd.Data;
using BackEnd.DTOs.Common;
using BackEnd.DTOs.ProductDTOs;
using BackEnd.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BackEnd.Helpers
{
    public static class ProductQueryHelper
    {
        public static readonly Expression<Func<Product, ProductDto>> AdminProjection = p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Slug = p.Slug,
            Sku = p.Sku,
            ShortDescription = p.ShortDescription,
            Description = p.Description,
            SeoTitle = p.SeoTitle,
            MetaDescription = p.MetaDescription,
            Price = p.Price,
            SalePrice = p.SalePrice,
            StockQuantity = p.StockQuantity,
            HasImage = p.ImgPath != null,
            IsActive = p.IsActive,
            Brand = p.Brand == null ? null : new BrandSummaryDto { Id = p.Brand.Id, Name = p.Brand.Name },
            Categories = p.Categories.Select(c => new CategorySummaryDto { Id = c.Id, Name = c.Name, Slug = c.Slug }).ToList(),
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            RowVersion = p.RowVersion
        };

        public static readonly Expression<Func<Product, PublicProductDto>> PublicProjection = p => new PublicProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Slug = p.Slug,
            ShortDescription = p.ShortDescription,
            Description = p.Description,
            SeoTitle = p.SeoTitle,
            MetaDescription = p.MetaDescription,
            Price = p.Price,
            SalePrice = p.SalePrice,
            InStock = p.StockQuantity > 0,
            HasImage = p.ImgPath != null,
            Brand = p.Brand == null ? null : new BrandSummaryDto { Id = p.Brand.Id, Name = p.Brand.Name },
            Categories = p.Categories.Select(c => new CategorySummaryDto { Id = c.Id, Name = c.Name, Slug = c.Slug }).ToList()
        };

        public static async Task<List<int>?> ResolveCategoryIdsAsync(AppDbContext context, int? categoryId, bool onlyActive)
        {
            if (!categoryId.HasValue)
            {
                return null;
            }

            var query = context.ProductCategories.AsNoTracking();
            if (onlyActive)
            {
                query = query.Where(c => c.IsActive);
            }

            var nodes = await query.Select(c => new { c.Id, c.ParentId }).ToListAsync();

            if (!nodes.Any(n => n.Id == categoryId.Value))
            {
                return new List<int>();
            }

            var ids = CategoryTreeHelper.GetDescendantIds(nodes.Select(n => (n.Id, n.ParentId)), categoryId.Value);
            ids.Add(categoryId.Value);

            return ids.ToList();
        }

        public static IQueryable<Product> ApplyFilters(IQueryable<Product> query, ProductQueryDto q, IReadOnlyCollection<int>? categoryIds)
        {
            if (!string.IsNullOrWhiteSpace(q.Search))
            {
                var search = q.Search.Trim();
                query = query.Where(p => p.Name.Contains(search) || (p.Sku != null && p.Sku.Contains(search)));
            }

            if (q.BrandId.HasValue)
            {
                query = query.Where(p => p.BrandId == q.BrandId.Value);
            }

            if (categoryIds is not null)
            {
                query = query.Where(p => p.Categories.Any(c => categoryIds.Contains(c.Id)));
            }

            foreach (var group in ParseAttributeFilters(q.Attr))
            {
                var attributeId = group.Key;
                var values = group.Value;

                query = query.Where(p => p.AttributeValues.Any(v => v.AttributeId == attributeId && values.Contains(v.Value)));
            }

            return query;
        }

        private static Dictionary<int, List<string>> ParseAttributeFilters(IEnumerable<string>? raw)
        {
            var result = new Dictionary<int, List<string>>();

            if (raw is null)
            {
                return result;
            }

            foreach (var item in raw.Take(10))
            {
                var separator = item.IndexOf(':');

                if (separator <= 0 || !int.TryParse(item[..separator], out var attributeId))
                {
                    continue;
                }

                var value = item[(separator + 1)..].Trim();

                if (value.Length == 0 || value.Length > 250)
                {
                    continue;
                }

                if (!result.TryGetValue(attributeId, out var values))
                {
                    values = new List<string>();
                    result[attributeId] = values;
                }

                values.Add(value);
            }

            return result;
        }

        public static async Task<PagedResult<T>> ToPagedAsync<T>(IQueryable<Product> query, ProductQueryDto q, Expression<Func<Product, T>> projection)
        {
            var page = Math.Max(q.Page, 1);
            var pageSize = Math.Clamp(q.PageSize, 1, 100);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(projection).ToListAsync();

            return new PagedResult<T>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            };
        }
    }
}