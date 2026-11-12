namespace BackEnd.DTOs.PostCategories
{
    public class PostCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? SeoTitle { get; set; }
        public string? MetaDescription { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public int PostsCount { get; set; }
    }
}