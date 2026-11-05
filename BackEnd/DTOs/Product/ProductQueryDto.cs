namespace BackEnd.DTOs.ProductDTOs
{
    public class ProductQueryDto
    {
        public string? Search { get; set; }
        public int? BrandId { get; set; }
        public int? CategoryId { get; set; }
        public bool? IsActive { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}