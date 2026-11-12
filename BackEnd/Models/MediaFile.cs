using System.ComponentModel.DataAnnotations;

namespace BackEnd.Models
{
    public class MediaFile
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string StoredName { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string OriginalName { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? AltText { get; set; }

        public string? UploadedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
