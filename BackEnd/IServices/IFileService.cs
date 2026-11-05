namespace BackEnd.IServices
{
    public interface IFileService
    {
        // Product
        Task<string> SaveProductImageAsync(IFormFile image);

        Task DeleteProductImageAsync(string? imagePath);

        Task<(Stream Stream, string ContentType)?> OpenProductImageAsync(string? imagePath);

        Task<string> SaveBrandLogoAsync(IFormFile image);

        Task DeleteBrandLogoAsync(string? imagePath);

        Task<(Stream Stream, string ContentType)?> OpenBrandLogoAsync(string? imagePath);

        // Product Category

        Task<string> SaveCategoryImageAsync(IFormFile image);

        Task DeleteCategoryImageAsync(string? imagePath);

        Task<(Stream Stream, string ContentType)?> OpenCategoryImageAsync(string? imagePath);
    }
}