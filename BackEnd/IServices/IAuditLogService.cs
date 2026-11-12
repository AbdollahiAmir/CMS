using BackEnd.DTOs.AuditLogs;
using BackEnd.DTOs.Common;

namespace BackEnd.IServices
{
    public interface IAuditLogService
    {
        Task<PagedResult<AuditLogDto>> GetAllAsync(AuditLogQueryDto query);
        Task<int> DeleteOlderThanAsync(DateTime threshold);
    }
}
