using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BackEnd.Models
{
    public class ProductCategory
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Slug { get; set; } = string.Empty;

        public string? Description { get; set; }

        [MaxLength(250)]
        public string? SeoTitle { get; set; }

        [MaxLength(500)]
        public string? MetaDescription { get; set; }

        public string? ImgPath { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public int? ParentId { get; set; }

        [JsonIgnore]
        public ProductCategory? Parent { get; set; }

        public ICollection<ProductCategory> Children { get; set; } = new List<ProductCategory>();

        [JsonIgnore]
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}