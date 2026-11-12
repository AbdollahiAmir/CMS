using BackEnd.DTOs.Common;
using BackEnd.DTOs.Pages;

namespace BackEnd.IServices
{
    public interface IContentPageService
    {
        Task<PagedResult<PageListItemDto>> GetAllAsync(PageQueryDto query);
        Task<ContentPageDto?> GetByIdAsync(int id);
        Task<ContentPageDto> CreateAsync(CreateContentPageDto dto, string? userId);
        Task<ContentPageDto?> PatchAsync(int id, PatchContentPageDto dto, string? userId);
        Task<ContentPageDto?> SetPublicationAsync(int id, bool isPublished, string? userId, string? rowVersion);
        Task<ContentPageDto?> SetScheduleAsync(int id, DateTimeOffset? scheduledAt, string? userId, string? rowVersion);
        Task<bool> DeleteAsync(int id, string? rowVersion);
    }
}