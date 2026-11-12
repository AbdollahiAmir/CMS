using System.ComponentModel.DataAnnotations;

namespace BackEnd.DTOs.Settings
{
    public class SiteSettingsDto
    {
        public string? SiteName { get; set; }
        public string? Tagline { get; set; }
        public string? LogoUrl { get; set; }
        public string? FaviconUrl { get; set; }
        public string? DefaultSeoTitle { get; set; }
        public string? DefaultMetaDescription { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? Address { get; set; }
        public string? InstagramUrl { get; set; }
        public string? TelegramUrl { get; set; }
        public string? WhatsappUrl { get; set; }
        public string? LinkedinUrl { get; set; }
        public string? XUrl { get; set; }
        public string? FooterText { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class UpdateSiteSettingsDto
    {
        [MaxLength(150, ErrorMessage = "نام سایت نمی‌تواند بیشتر از ۱۵۰ کاراکتر باشد")]
        public string? SiteName { get; set; }

        [MaxLength(250, ErrorMessage = "شعار سایت نمی‌تواند بیشتر از ۲۵۰ کاراکتر باشد")]
        public string? Tagline { get; set; }

        [MaxLength(500, ErrorMessage = "آدرس لوگو نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد")]
        public string? LogoUrl { get; set; }

        [MaxLength(500, ErrorMessage = "آدرس فاویکن نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد")]
        public string? FaviconUrl { get; set; }

        [MaxLength(250, ErrorMessage = "عنوان سئوی پیش‌فرض نمی‌تواند بیشتر از ۲۵۰ کاراکتر باشد")]
        public string? DefaultSeoTitle { get; set; }

        [MaxLength(500, ErrorMessage = "توضیحات متای پیش‌فرض نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد")]
        public string? DefaultMetaDescription { get; set; }

        [MaxLength(200, ErrorMessage = "ایمیل نمی‌تواند بیشتر از ۲۰۰ کاراکتر باشد")]
        [EmailAddress(ErrorMessage = "ایمیل معتبر نیست")]
        public string? ContactEmail { get; set; }

        [MaxLength(50, ErrorMessage = "شماره تماس نمی‌تواند بیشتر از ۵۰ کاراکتر باشد")]
        public string? ContactPhone { get; set; }

        [MaxLength(500, ErrorMessage = "آدرس نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد")]
        public string? Address { get; set; }

        [MaxLength(300, ErrorMessage = "آدرس اینستاگرام نمی‌تواند بیشتر از ۳۰۰ کاراکتر باشد")]
        public string? InstagramUrl { get; set; }

        [MaxLength(300, ErrorMessage = "آدرس تلگرام نمی‌تواند بیشتر از ۳۰۰ کاراکتر باشد")]
        public string? TelegramUrl { get; set; }

        [MaxLength(300, ErrorMessage = "آدرس واتساپ نمی‌تواند بیشتر از ۳۰۰ کاراکتر باشد")]
        public string? WhatsappUrl { get; set; }

        [MaxLength(300, ErrorMessage = "آدرس لینکدین نمی‌تواند بیشتر از ۳۰۰ کاراکتر باشد")]
        public string? LinkedinUrl { get; set; }

        [MaxLength(300, ErrorMessage = "آدرس ایکس نمی‌تواند بیشتر از ۳۰۰ کاراکتر باشد")]
        public string? XUrl { get; set; }

        [MaxLength(1000, ErrorMessage = "متن فوتر نمی‌تواند بیشتر از ۱۰۰۰ کاراکتر باشد")]
        public string? FooterText { get; set; }
    }
}
