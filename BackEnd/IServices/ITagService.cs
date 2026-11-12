using BackEnd.DTOs.Tags;

namespace BackEnd.IServices
{
    public interface ITagService
    {
        Task<List<TagDto>> GetAllAsync();
        Task<TagDto?> GetByIdAsync(int id);
        Task<TagDto> CreateAsync(CreateTagDto dto);
        Task<TagDto?> PatchAsync(int id, PatchTagDto dto);
        Task<bool> DeleteAsync(int id);
    }
}