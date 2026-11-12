namespace BackEnd.DTOs.Media
{
    public class MediaDto
    {
        public int Id { get; set; }
        public string OriginalName { get; set; } = string.Empty;
        public string? AltText { get; set; }
        public string Url { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string? UploadedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
