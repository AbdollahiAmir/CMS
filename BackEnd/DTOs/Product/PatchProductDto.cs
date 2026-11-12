using System.ComponentModel.DataAnnotations;

namespace BackEnd.DTOs.ProductDTOs
{
    public class PatchProductDto
    {
        [Required(ErrorMessage = "نسخه اطلاعات الزامی است")]
        public string RowVersion { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Name { get; set; }

        [MaxLength(250)]
        public string? Slug { get; set; }

        [MaxLength(100)]
        public string? Sku { get; set; }

        [MaxLength(500)]
        public string? ShortDescription { get; set; }

        public string? Description { get; set; }

        [MaxLength(250)]
        public string? SeoTitle { get; set; }

        [MaxLength(500)]
        public string? MetaDescription { get; set; }

        [Range(0.01, 999999999999)]
        public decimal? Price { get; set; }

        [Range(0, 999999999999)]
        public decimal? SalePrice { get; set; }

        [Range(0, int.MaxValue)]
        public int? StockQuantity { get; set; }

        [Range(0, int.MaxValue)]
        public int? BrandId { get; set; }

        public List<int>? CategoryIds { get; set; }

        public IFormFile? ImgFile { get; set; }
    }
}