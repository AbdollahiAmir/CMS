using BackEnd.Data;
using BackEnd.DTOs.Revisions;
using BackEnd.IServices;
using BackEnd.Models;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Services
{
    public class RevisionService : IRevisionService
    {
        private const int MaxRevisionsPerEntity = 50;

        private readonly AppDbContext _context;

        public RevisionService(AppDbContext context)
        {
            _context = context;
        }

        public void Add(
            string entityType,
            int entityId,
            string title,
            string slug,
            string? excerpt,
            string content,
            string? seoTitle,
            string? metaDescription,
            string action,
            string? userId)
        {
            _context.ContentRevisions.Add(new ContentRevision
            {
                EntityType = entityType,
                EntityId = entityId,
                Title = title,
                Slug = slug,
                Excerpt = excerpt,
                Content = content,
                SeoTitle = seoTitle,
                MetaDescription = metaDescription,
                Action = action,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            });
        }

        public async Task PruneAsync(string entityType, int entityId)
        {
            var staleIds = await _context.ContentRevisions
                .Where(r => r.EntityType == entityType && r.EntityId == entityId)
                .OrderByDescending(r => r.CreatedAt)
                .ThenByDescending(r => r.Id)
                .Skip(MaxRevisionsPerEntity)
                .Select(r => r.Id)
                .ToListAsync();

            if (staleIds.Count == 0)
            {
                return;
            }

            await _context.ContentRevisions
                .Where(r => staleIds.Contains(r.Id))
                .ExecuteDeleteAsync();
        }

        public async Task<List<RevisionListItemDto>> ListAsync(string entityType, int entityId)
        {
            return await _context.ContentRevisions
                .AsNoTracking()
                .Where(r => r.EntityType == entityType && r.EntityId == entityId)
                .OrderByDescending(r => r.CreatedAt)
                .ThenByDescending(r => r.Id)
                .Select(r => new RevisionListItemDto
                {
                    Id = r.Id,
                    Title = r.Title,
                    Action = r.Action,
                    CreatedByUserId = r.CreatedByUserId,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<RevisionDto?> GetAsync(string entityType, int entityId, int revisionId)
        {
            return await _context.ContentRevisions
                .AsNoTracking()
                .Where(r => r.Id == revisionId && r.EntityType == entityType && r.EntityId == entityId)
                .Select(r => new RevisionDto
                {
                    Id = r.Id,
                    Title = r.Title,
                    Slug = r.Slug,
                    Excerpt = r.Excerpt,
                    Content = r.Content,
                    SeoTitle = r.SeoTitle,
                    MetaDescription = r.MetaDescription,
                    Action = r.Action,
                    CreatedByUserId = r.CreatedByUserId,
                    CreatedAt = r.CreatedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task DeleteAllAsync(string entityType, int entityId)
        {
            await _context.ContentRevisions
                .Where(r => r.EntityType == entityType && r.EntityId == entityId)
                .ExecuteDeleteAsync();
        }
    }
}
