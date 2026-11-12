using BackEnd.DTOs.Settings;

namespace BackEnd.IServices
{
    public interface ISiteSettingsService
    {
        Task<SiteSettingsDto> GetAsync();
        Task<SiteSettingsDto> UpdateAsync(UpdateSiteSettingsDto dto, string? userId);
    }
}
