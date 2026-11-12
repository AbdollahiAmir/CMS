namespace BackEnd.DTOs.Posts
{
    public class PostDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Excerpt { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? FeaturedImagePath { get; set; }
        public string? SeoTitle { get; set; }
        public string? MetaDescription { get; set; }
        public bool IsPublished { get; set; }
        public DateTime? PublishedAt { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public List<TagSummaryDto> Tags { get; set; } = [];
        public string? AuthorId { get; set; }
        public string? UpdatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string RowVersion { get; set; } = string.Empty;
    }

    public class TagSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
    }
}