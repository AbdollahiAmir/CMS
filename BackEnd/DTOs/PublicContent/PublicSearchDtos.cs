namespace BackEnd.DTOs.PublicContent
{
    public class PublicSearchQueryDto
    {
        public string? Q { get; set; }
        public int Limit { get; set; } = 5;
    }

    public class PublicSearchPageDto
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Excerpt { get; set; }
    }

    public class PublicSearchProductDto
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? SalePrice { get; set; }
        public bool InStock { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class PublicSearchResultDto
    {
        public string Query { get; set; } = string.Empty;
        public List<PublicPostListItemDto> Posts { get; set; } = [];
        public List<PublicSearchPageDto> Pages { get; set; } = [];
        public List<PublicSearchProductDto> Products { get; set; } = [];
    }
}
