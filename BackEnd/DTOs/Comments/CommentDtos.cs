using System.ComponentModel.DataAnnotations;
using BackEnd.Models;

namespace BackEnd.DTOs.Comments
{
    public class CreateCommentDto
    {
        [MaxLength(100)]
        public string? AuthorName { get; set; }

        [EmailAddress(ErrorMessage = "ایمیل نامعتبر است")]
        [MaxLength(256)]
        public string? AuthorEmail { get; set; }

        [Required(ErrorMessage = "متن نظر الزامی است")]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Website { get; set; }
    }

    public class PublicCommentDto
    {
        public int Id { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CommentQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class AdminCommentDto
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public string PostTitle { get; set; } = string.Empty;
        public string PostSlug { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string? AuthorEmail { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public CommentStatus Status { get; set; }
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModeratedAt { get; set; }
        public string? ModeratedByUserId { get; set; }
    }

    public class AdminCommentQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public CommentStatus? Status { get; set; }
        public int? PostId { get; set; }
        public string? Search { get; set; }
    }

    public class SetCommentStatusDto
    {
        [Required]
        public CommentStatus Status { get; set; }
    }

    public class BulkCommentStatusDto
    {
        [Required, MinLength(1), MaxLength(100)]
        public List<int> Ids { get; set; } = new();

        [Required]
        public CommentStatus Status { get; set; }
    }
}
