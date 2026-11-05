namespace BackEnd.DTOs.Category
{
    public class ProductCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? SeoTitle { get; set; }
        public string? MetaDescription { get; set; }
        public bool HasImage { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public int? ParentId { get; set; }
        public List<ProductCategoryDto> Children { get; set; } = new();
    }
}