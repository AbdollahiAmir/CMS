using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackEnd.Models
{
    public class SiteSetting
    {
        public const int SingletonId = 1;

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; } = SingletonId;

        [MaxLength(150)]
        public string? SiteName { get; set; }

        [MaxLength(250)]
        public string? Tagline { get; set; }

        [MaxLength(500)]
        public string? LogoUrl { get; set; }

        [MaxLength(500)]
        public string? FaviconUrl { get; set; }

        [MaxLength(250)]
        public string? DefaultSeoTitle { get; set; }

        [MaxLength(500)]
        public string? DefaultMetaDescription { get; set; }

        [MaxLength(200)]
        public string? ContactEmail { get; set; }

        [MaxLength(50)]
        public string? ContactPhone { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(300)]
        public string? InstagramUrl { get; set; }

        [MaxLength(300)]
        public string? TelegramUrl { get; set; }

        [MaxLength(300)]
        public string? WhatsappUrl { get; set; }

        [MaxLength(300)]
        public string? LinkedinUrl { get; set; }

        [MaxLength(300)]
        public string? XUrl { get; set; }

        [MaxLength(1000)]
        public string? FooterText { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string? UpdatedByUserId { get; set; }
    }
}
