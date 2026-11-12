namespace BackEnd.DTOs.Media
{
    public class MediaQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 24;
        public string? Search { get; set; }
    }
}
