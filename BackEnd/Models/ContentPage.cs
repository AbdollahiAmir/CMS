
using System.ComponentModel.DataAnnotations;

namespace BackEnd.Models
{
    public class ContentPage
    {
        public int Id { get; set; }

        [Required, MaxLength(250)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(250)]
        public string Slug { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Excerpt { get; set; }

        [MaxLength(250)]
        public string? SeoTitle { get; set; }

        [MaxLength(500)]
        public string? MetaDescription { get; set; }

        public bool IsPublished { get; set; }

        public DateTime? PublishedAt { get; set; }

        public DateTime? ScheduledAt { get; set; }

        public string? CreatedByUserId { get; set; }

        public string? UpdatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = [];
    }
}
