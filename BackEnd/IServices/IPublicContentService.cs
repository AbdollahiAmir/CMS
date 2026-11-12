using BackEnd.DTOs.Common;
using BackEnd.DTOs.PublicContent;

namespace BackEnd.IServices
{
    public interface IPublicContentService
    {
        Task<List<PublicPageListItemDto>> GetPagesAsync();

        Task<PublicPageDto?> GetPageBySlugAsync(string slug);

        Task<PagedResult<PublicPostListItemDto>> GetPostsAsync(
            PublicPostQueryDto query);

        Task<PublicPostDto?> GetPostBySlugAsync(string slug);

        Task<PublicPostDto?> GetPostPreviewAsync(int id);

        Task<PublicPageDto?> GetPagePreviewAsync(int id);

        Task<List<PublicCategoryDto>> GetCategoriesAsync();

        Task<List<PublicTagDto>> GetTagsAsync();

        Task<(Stream Stream, string ContentType)?> GetPostFeaturedImageAsync(string slug, bool thumbnail = false);
    }
}