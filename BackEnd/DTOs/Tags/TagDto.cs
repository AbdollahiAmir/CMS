namespace BackEnd.DTOs.Tags
{
    public class TagDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int PostsCount { get; set; }
    }
}