using BackEnd.Data;
using BackEnd.DTOs.Settings;
using BackEnd.Helpers;
using BackEnd.IServices;
using BackEnd.Models;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Services
{
    public class SiteSettingsService : ISiteSettingsService
    {
        private readonly AppDbContext _context;

        public SiteSettingsService(AppDbContext context)
        {
            _context = context;
        }

        private static string? Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static SiteSettingsDto Map(SiteSetting? setting)
        {
            if (setting is null)
            {
                return new SiteSettingsDto();
            }

            return new SiteSettingsDto
            {
                SiteName = setting.SiteName,
                Tagline = setting.Tagline,
                LogoUrl = setting.LogoUrl,
                FaviconUrl = setting.FaviconUrl,
                DefaultSeoTitle = setting.DefaultSeoTitle,
                DefaultMetaDescription = setting.DefaultMetaDescription,
                ContactEmail = setting.ContactEmail,
                ContactPhone = setting.ContactPhone,
                Address = setting.Address,
                InstagramUrl = setting.InstagramUrl,
                TelegramUrl = setting.TelegramUrl,
                WhatsappUrl = setting.WhatsappUrl,
                LinkedinUrl = setting.LinkedinUrl,
                XUrl = setting.XUrl,
                FooterText = setting.FooterText,
                UpdatedAt = setting.UpdatedAt
            };
        }

        public async Task<SiteSettingsDto> GetAsync()
        {
            var setting = await _context.SiteSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == SiteSetting.SingletonId);

            return Map(setting);
        }

        public async Task<SiteSettingsDto> UpdateAsync(UpdateSiteSettingsDto dto, string? userId)
        {
            var logoUrl = UrlValidator.Normalize(dto.LogoUrl, "آدرس لوگو");
            var faviconUrl = UrlValidator.Normalize(dto.FaviconUrl, "آدرس فاویکن");
            var instagramUrl = UrlValidator.Normalize(dto.InstagramUrl, "آدرس اینستاگرام", allowRelative: false);
            var telegramUrl = UrlValidator.Normalize(dto.TelegramUrl, "آدرس تلگرام", allowRelative: false);
            var whatsappUrl = UrlValidator.Normalize(dto.WhatsappUrl, "آدرس واتساپ", allowRelative: false);
            var linkedinUrl = UrlValidator.Normalize(dto.LinkedinUrl, "آدرس لینکدین", allowRelative: false);
            var xUrl = UrlValidator.Normalize(dto.XUrl, "آدرس ایکس", allowRelative: false);

            var setting = await _context.SiteSettings
                .FirstOrDefaultAsync(s => s.Id == SiteSetting.SingletonId);

            if (setting is null)
            {
                setting = new SiteSetting { Id = SiteSetting.SingletonId };
                _context.SiteSettings.Add(setting);
            }

            setting.SiteName = Clean(dto.SiteName);
            setting.Tagline = Clean(dto.Tagline);
            setting.LogoUrl = logoUrl;
            setting.FaviconUrl = faviconUrl;
            setting.DefaultSeoTitle = Clean(dto.DefaultSeoTitle);
            setting.DefaultMetaDescription = Clean(dto.DefaultMetaDescription);
            setting.ContactEmail = Clean(dto.ContactEmail);
            setting.ContactPhone = Clean(dto.ContactPhone);
            setting.Address = Clean(dto.Address);
            setting.InstagramUrl = instagramUrl;
            setting.TelegramUrl = telegramUrl;
            setting.WhatsappUrl = whatsappUrl;
            setting.LinkedinUrl = linkedinUrl;
            setting.XUrl = xUrl;
            setting.FooterText = Clean(dto.FooterText);
            setting.UpdatedByUserId = userId;
            setting.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Map(setting);
        }
    }
}
