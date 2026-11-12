using System.ComponentModel.DataAnnotations;

namespace BackEnd.Models
{
    public static class RevisionTypes
    {
        public const string Post = "Post";
        public const string Page = "Page";
    }

    public static class RevisionActions
    {
        public const string Create = "create";
        public const string Update = "update";
        public const string Restore = "restore";
    }

    public class ContentRevision
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string EntityType { get; set; } = string.Empty;

        public int EntityId { get; set; }

        [Required, MaxLength(250)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(250)]
        public string Slug { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Excerpt { get; set; }

        public string Content { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? SeoTitle { get; set; }

        [MaxLength(500)]
        public string? MetaDescription { get; set; }

        [Required, MaxLength(20)]
        public string Action { get; set; } = string.Empty;

        public string? CreatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
