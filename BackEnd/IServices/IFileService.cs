namespace BackEnd.IServices
{
    public interface IFileService
    {
        Task<string> SaveProductImageAsync(IFormFile image);
        Task DeleteProductImageAsync(string? imagePath);
        Task<(Stream Stream, string ContentType)?> OpenProductImageAsync(string? imagePath, bool thumbnail = false);

        Task<string> SaveBrandLogoAsync(IFormFile image);
        Task DeleteBrandLogoAsync(string? imagePath);
        Task<(Stream Stream, string ContentType)?> OpenBrandLogoAsync(string? imagePath, bool thumbnail = false);

        Task<string> SaveCategoryImageAsync(IFormFile image);
        Task DeleteCategoryImageAsync(string? imagePath);
        Task<(Stream Stream, string ContentType)?> OpenCategoryImageAsync(string? imagePath, bool thumbnail = false);

        Task<string> SavePostFeaturedImageAsync(IFormFile image);
        Task DeletePostFeaturedImageAsync(string? imagePath);
        Task<(Stream Stream, string ContentType)?> OpenPostFeaturedImageAsync(string? imagePath, bool thumbnail = false);
    }
}