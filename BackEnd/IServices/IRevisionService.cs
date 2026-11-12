using BackEnd.DTOs.Revisions;

namespace BackEnd.IServices
{
    public interface IRevisionService
    {
        void Add(
            string entityType,
            int entityId,
            string title,
            string slug,
            string? excerpt,
            string content,
            string? seoTitle,
            string? metaDescription,
            string action,
            string? userId);

        Task PruneAsync(string entityType, int entityId);
        Task<List<RevisionListItemDto>> ListAsync(string entityType, int entityId);
        Task<RevisionDto?> GetAsync(string entityType, int entityId, int revisionId);
        Task DeleteAllAsync(string entityType, int entityId);
    }
}
