namespace BackEnd.DTOs.PublicContent
{
    public class PublicPostQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public string? CategorySlug { get; set; }
        public string? TagSlug { get; set; }
    }
}