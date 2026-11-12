namespace BackEnd.DTOs.Posts
{
    public class PostListItemDto
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
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? AuthorId { get; set; }
    }
}