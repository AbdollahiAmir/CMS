namespace BackEnd.DTOs.PublicContent
{
    public class PublicPostListItemDto
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Excerpt { get; set; }
        public string? FeaturedImageUrl { get; set; }
        public string? CategoryName { get; set; }
        public string? CategorySlug { get; set; }
        public DateTime? PublishedAt { get; set; }
        public List<PublicTagDto> Tags { get; set; } = [];
    }
}