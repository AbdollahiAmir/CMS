using BackEnd.DTOs.Common;
using BackEnd.DTOs.Media;

namespace BackEnd.IServices
{
    public interface IMediaService
    {
        Task<PagedResult<MediaDto>> GetAllAsync(MediaQueryDto query);
        Task<MediaDto?> GetByIdAsync(int id);
        Task<MediaDto> UploadAsync(IFormFile image, string? altText, string? userId);
        Task<MediaDto?> UpdateAsync(int id, UpdateMediaDto dto);
        Task<bool> DeleteAsync(int id);
        Task<(Stream Stream, string ContentType)?> OpenFileAsync(string fileName, bool thumbnail = false);
    }
}
