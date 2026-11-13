namespace BackEnd.DTOs.ProductDTOs
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public string? ShortDescription { get; set; }
        public string? Description { get; set; }
        public string? SeoTitle { get; set; }
        public string? MetaDescription { get; set; }
        public decimal Price { get; set; }
        public decimal? SalePrice { get; set; }
        public int StockQuantity { get; set; }
        public bool HasImage { get; set; }
        public bool IsActive { get; set; }
        public BrandSummaryDto? Brand { get; set; }
        public List<CategorySummaryDto> Categories { get; set; } = new();
        public List<ProductImageDto> Images { get; set; } = new();
        public List<ProductAttributeValueDto> Attributes { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public byte[] RowVersion { get; set; } = [];
    }
}