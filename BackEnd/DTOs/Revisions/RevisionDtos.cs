using System.ComponentModel.DataAnnotations;

namespace BackEnd.DTOs.Revisions
{
    public class RevisionListItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? CreatedByUserId { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RevisionDto : RevisionListItemDto
    {
        public string Slug { get; set; } = string.Empty;
        public string? Excerpt { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? SeoTitle { get; set; }
        public string? MetaDescription { get; set; }
    }

    public class RestoreRevisionDto
    {
        [Required(ErrorMessage = "نسخه اطلاعات الزامی است")]
        public string RowVersion { get; set; } = string.Empty;
    }
}
