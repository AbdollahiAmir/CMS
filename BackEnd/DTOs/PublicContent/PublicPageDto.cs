namespace BackEnd.DTOs.PublicContent
{
    public class PublicPageDto
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Excerpt { get; set; }
        public string? SeoTitle { get; set; }
        public string? MetaDescription { get; set; }
        public DateTime? PublishedAt { get; set; }
    }
}