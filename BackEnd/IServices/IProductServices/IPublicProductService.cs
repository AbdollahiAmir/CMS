using BackEnd.DTOs.Common;
using BackEnd.DTOs.ProductDTOs;

namespace BackEnd.IServices.IProductServices
{
    public interface IPublicProductService
    {
        Task<PagedResult<PublicProductDto>> GetProductsAsync(ProductQueryDto query);
        Task<PublicProductDto?> GetBySlugAsync(string slug);
        Task<List<CartLookupItemDto>> LookupForCartAsync(IReadOnlyCollection<int> productIds);
        Task<(Stream Stream, string ContentType)?> GetProductImageAsync(int id, bool thumbnail = false);
    }
}