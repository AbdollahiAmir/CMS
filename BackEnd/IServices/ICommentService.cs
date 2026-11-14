using BackEnd.DTOs.Comments;
using BackEnd.DTOs.Common;
using BackEnd.Models;

namespace BackEnd.IServices
{
    public interface ICommentService
    {
        Task<PagedResult<PublicCommentDto>?> GetApprovedAsync(string postSlug, CommentQueryDto query);
        Task<bool?> SubmitAsync(string postSlug, CreateCommentDto dto, string? userId, string? userName, string? ipAddress);
        Task<PagedResult<AdminCommentDto>> GetAllAsync(AdminCommentQueryDto query);
        Task<int> GetPendingCountAsync();
        Task<AdminCommentDto?> SetStatusAsync(int id, CommentStatus status, string? moderatorId);
        Task<int> SetStatusBulkAsync(IReadOnlyList<int> ids, CommentStatus status, string? moderatorId);
        Task<bool> DeleteAsync(int id);
    }
}
