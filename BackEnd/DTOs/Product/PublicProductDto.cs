namespace BackEnd.DTOs.ProductDTOs
{
    public class PublicProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? ShortDescription { get; set; }
        public string? Description { get; set; }
        public string? SeoTitle { get; set; }
        public string? MetaDescription { get; set; }
        public decimal Price { get; set; }
        public decimal? SalePrice { get; set; }
        public bool InStock { get; set; }
        public bool HasImage { get; set; }
        public BrandSummaryDto? Brand { get; set; }
        public List<CategorySummaryDto> Categories { get; set; } = new();
    }
}