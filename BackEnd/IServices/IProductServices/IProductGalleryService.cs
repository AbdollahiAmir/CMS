using BackEnd.DTOs.ProductDTOs;

namespace BackEnd.IServices.IProductServices
{
    public interface IProductGalleryService
    {
        Task<List<ProductImageDto>> GetImagesAsync(int productId, bool publicUrls);
        Task<ProductImageDto?> AddAsync(int productId, IFormFile image, string? altText);
        Task<ProductImageDto?> UpdateAsync(int productId, int imageId, string? altText);
        Task<List<ProductImageDto>?> ReorderAsync(int productId, IReadOnlyList<int> imageIds);
        Task<bool> DeleteAsync(int productId, int imageId);
        Task<(Stream Stream, string ContentType)?> OpenAsync(int productId, int imageId, bool thumbnail, bool onlyActive);
    }
}
