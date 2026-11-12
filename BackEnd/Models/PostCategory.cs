
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BackEnd.Models
{
    public class PostCategory
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Slug { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(250)]
        public string? SeoTitle { get; set; }

        [MaxLength(500)]
        public string? MetaDescription { get; set; }

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; }

        [JsonIgnore]
        public ICollection<Post> Posts { get; set; } = new List<Post>();
    }
}
