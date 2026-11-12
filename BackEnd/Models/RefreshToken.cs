using System.ComponentModel.DataAnnotations;

namespace BackEnd.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public AppUser User { get; set; } = null!;

        [Required, MaxLength(88)]
        public string TokenHash { get; set; } = string.Empty;

        public Guid FamilyId { get; set; }

        public DateTime FamilyCreatedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        [MaxLength(88)]
        public string? ReplacedByTokenHash { get; set; }
    }
}