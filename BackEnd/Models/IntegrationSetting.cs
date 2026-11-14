using System.ComponentModel.DataAnnotations;

namespace BackEnd.Models
{
    public class IntegrationSetting
    {
        [Key, MaxLength(50)]
        public string Key { get; set; } = string.Empty;

        [Required]
        public string ProtectedValue { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(450)]
        public string? UpdatedByUserId { get; set; }
    }
}
