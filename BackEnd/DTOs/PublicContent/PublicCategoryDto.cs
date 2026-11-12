namespace BackEnd.DTOs.PublicContent
{
    public class PublicCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}