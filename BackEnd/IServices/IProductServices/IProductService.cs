using BackEnd.DTOs.Common;
using BackEnd.DTOs.ProductDTOs;

namespace BackEnd.IServices.IProductServices
{
    public interface IProductService
    {
        Task<PagedResult<ProductDto>> GetAllAsync(ProductQueryDto query);

        Task<ProductDto?> GetByIdAsync(int id);

        Task<ProductDto> CreateAsync(CreateProductDto dto);

        Task<ProductDto?> PatchAsync(int id, PatchProductDto dto);

        Task<bool> DeleteAsync(int id);

        Task<ProductDto?> SetActiveAsync(int id, bool isActive);

        Task<(Stream Stream, string ContentType)?> GetProductImageAsync(int id, bool thumbnail = false);
    }
}