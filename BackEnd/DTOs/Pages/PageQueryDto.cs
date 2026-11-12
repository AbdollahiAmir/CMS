namespace BackEnd.DTOs.Pages
{
    public class PageQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public bool? IsPublished { get; set; }
    }
}