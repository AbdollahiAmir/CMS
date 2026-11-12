namespace BackEnd.DTOs.PublicContent
{
    public class PublicPageListItemDto
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Excerpt { get; set; }
        public DateTime? PublishedAt { get; set; }
    }
}