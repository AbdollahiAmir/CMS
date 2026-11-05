using BackEnd.DTOs.Category;

namespace BackEnd.IServices
{
    public interface IProductCategoryService
    {
        Task<List<ProductCategoryDto>> GetTreeAsync(bool includeInactive);
        Task<ProductCategoryDto?> GetByIdAsync(int id);
        Task<ProductCategoryDto> CreateAsync(CreateProductCategoryDto dto);
        Task<ProductCategoryDto?> PatchAsync(int id, PatchProductCategoryDto dto);
        Task<bool> DeleteAsync(int id);
        Task<(Stream Stream, string ContentType)?> GetCategoryImageAsync(int id);
    }
}