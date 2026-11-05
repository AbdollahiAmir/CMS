using System.ComponentModel.DataAnnotations;

namespace BackEnd.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required, MaxLength(250)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(250)]
        public string Slug { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Sku { get; set; }

        [MaxLength(500)]
        public string? ShortDescription { get; set; }

        public string? Description { get; set; }

        [MaxLength(250)]
        public string? SeoTitle { get; set; }

        [MaxLength(500)]
        public string? MetaDescription { get; set; }

        public decimal Price { get; set; }

        public decimal? SalePrice { get; set; }

        public int StockQuantity { get; set; }

        public string? ImgPath { get; set; }

        public bool IsActive { get; set; } = false;

        public int? BrandId { get; set; }

        public Brand? Brand { get; set; }

        public ICollection<ProductCategory> Categories { get; set; } = new List<ProductCategory>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = [];
    }
}