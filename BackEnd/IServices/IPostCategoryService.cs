using BackEnd.DTOs.PostCategories;

namespace BackEnd.IServices
{
    public interface IPostCategoryService
    {
        Task<List<PostCategoryDto>> GetAllAsync(bool includeInactive);
        Task<PostCategoryDto?> GetByIdAsync(int id);
        Task<PostCategoryDto> CreateAsync(CreatePostCategoryDto dto);
        Task<PostCategoryDto?> PatchAsync(int id, PatchPostCategoryDto dto);
        Task<bool> DeleteAsync(int id);
    }
}