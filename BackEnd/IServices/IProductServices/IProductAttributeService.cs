using BackEnd.DTOs.ProductDTOs;

namespace BackEnd.IServices.IProductServices
{
    public interface IProductAttributeService
    {
        Task<List<ProductAttributeDto>> GetAllAsync();
        Task<ProductAttributeDto> CreateAsync(CreateProductAttributeDto dto);
        Task<ProductAttributeDto?> PatchAsync(int id, PatchProductAttributeDto dto);
        Task<bool> DeleteAsync(int id);
        Task<List<ProductAttributeValueDto>> GetValuesAsync(int productId);
        Task<List<ProductAttributeValueDto>?> SetValuesAsync(int productId, IReadOnlyList<ProductAttributeValueInputDto> items);
        Task<List<FilterAttributeDto>> GetFilterableAsync();
    }
}
