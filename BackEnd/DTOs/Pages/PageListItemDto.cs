namespace BackEnd.DTOs.Pages
{
    public class PageListItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Excerpt { get; set; }
        public bool IsPublished { get; set; }
        public DateTime? PublishedAt { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedByUserId { get; set; }
        public string? CreatedByName { get; set; }
        public string? UpdatedByUserId { get; set; }
        public string? UpdatedByName { get; set; }
    }
}