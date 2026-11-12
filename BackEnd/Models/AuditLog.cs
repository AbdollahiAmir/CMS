using System.ComponentModel.DataAnnotations;

namespace BackEnd.Models
{
    public class AuditLog
    {
        public long Id { get; set; }

        [MaxLength(450)]
        public string? UserId { get; set; }

        [MaxLength(256)]
        public string? UserName { get; set; }

        [Required, MaxLength(100)]
        public string Controller { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        [Required, MaxLength(10)]
        public string Method { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string Path { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? EntityId { get; set; }

        public int StatusCode { get; set; }

        [MaxLength(64)]
        public string? IpAddress { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
