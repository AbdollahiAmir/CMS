using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BackEnd.Models
{
    public class Brand
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Slug { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? LogoPath { get; set; }

        public bool IsActive { get; set; } = true;

        [JsonIgnore]
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}