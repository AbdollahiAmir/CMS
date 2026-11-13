using System.ComponentModel.DataAnnotations;

namespace BackEnd.Models
{
    public class ProductImage
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public Product Product { get; set; } = null!;

        [Required, MaxLength(500)]
        public string Path { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? AltText { get; set; }

        public int SortOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
