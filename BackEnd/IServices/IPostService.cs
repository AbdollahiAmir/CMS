using BackEnd.DTOs.Common;
using BackEnd.DTOs.Posts;

namespace BackEnd.IServices
{
    public interface IPostService
    {
        Task<PagedResult<PostListItemDto>> GetAllAsync(PostQueryDto query);
        Task<PostDto?> GetByIdAsync(int id);
        Task<PostDto> CreateAsync(CreatePostDto dto, string? userId);
        Task<PostDto?> PatchAsync(int id, PatchPostDto dto, string? userId);
        Task<PostDto?> SetPublicationAsync(int id, bool isPublished, string? userId, string? rowVersion);
        Task<bool> SaveFeaturedImageAsync(int id, IFormFile image, string? userId, string? rowVersion);
        Task<bool> DeleteFeaturedImageAsync(int id, string? userId, string? rowVersion);
        Task<(Stream Stream, string ContentType)?> GetFeaturedImageAsync(int id, bool thumbnail = false);
        Task<bool> DeleteAsync(int id, string? rowVersion);
    }
}