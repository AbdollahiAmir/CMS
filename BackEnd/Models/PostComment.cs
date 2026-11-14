using System.ComponentModel.DataAnnotations;

namespace BackEnd.Models
{
    public enum CommentStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2
    }

    public class PostComment
    {
        public int Id { get; set; }

        public int PostId { get; set; }

        public Post Post { get; set; } = null!;

        [Required, MaxLength(100)]
        public string AuthorName { get; set; } = string.Empty;

        [MaxLength(256)]
        public string? AuthorEmail { get; set; }

        [Required, MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        [MaxLength(450)]
        public string? UserId { get; set; }

        public CommentStatus Status { get; set; } = CommentStatus.Pending;

        [MaxLength(64)]
        public string? IpAddress { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ModeratedAt { get; set; }

        [MaxLength(450)]
        public string? ModeratedByUserId { get; set; }
    }
}
