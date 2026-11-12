using BackEnd.Data;
using BackEnd.DTOs.AuditLogs;
using BackEnd.DTOs.Common;
using BackEnd.IServices;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly AppDbContext _context;

        public AuditLogService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<AuditLogDto>> GetAllAsync(AuditLogQueryDto query)
        {
            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 200);

            var logs = _context.AuditLogs.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.UserId))
            {
                var userId = query.UserId.Trim();
                logs = logs.Where(l => l.UserId == userId);
            }

            if (!string.IsNullOrWhiteSpace(query.Controller))
            {
                var controller = query.Controller.Trim();
                logs = logs.Where(l => l.Controller == controller);
            }

            if (!string.IsNullOrWhiteSpace(query.EntityId))
            {
                var entityId = query.EntityId.Trim();
                logs = logs.Where(l => l.EntityId == entityId);
            }

            if (query.From.HasValue)
            {
                var from = query.From.Value.ToUniversalTime();
                logs = logs.Where(l => l.CreatedAt >= from);
            }

            if (query.To.HasValue)
            {
                var to = query.To.Value.ToUniversalTime();
                logs = logs.Where(l => l.CreatedAt <= to);
            }

            var totalCount = await logs.CountAsync();

            var items = await logs
                .OrderByDescending(l => l.CreatedAt)
                .ThenByDescending(l => l.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new AuditLogDto
                {
                    Id = l.Id,
                    UserId = l.UserId,
                    UserName = l.UserName,
                    Controller = l.Controller,
                    Action = l.Action,
                    Method = l.Method,
                    Path = l.Path,
                    EntityId = l.EntityId,
                    StatusCode = l.StatusCode,
                    IpAddress = l.IpAddress,
                    CreatedAt = l.CreatedAt
                })
                .ToListAsync();

            return new PagedResult<AuditLogDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<int> DeleteOlderThanAsync(DateTime threshold)
        {
            return await _context.AuditLogs
                .Where(l => l.CreatedAt < threshold)
                .ExecuteDeleteAsync();
        }
    }
}
