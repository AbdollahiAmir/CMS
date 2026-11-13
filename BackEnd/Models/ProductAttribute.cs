using System.ComponentModel.DataAnnotations;

namespace BackEnd.Models
{
    public class ProductAttribute
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Slug { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        public bool IsFilterable { get; set; }
    }
}
