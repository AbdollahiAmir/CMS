namespace BackEnd.DTOs.AuditLogs
{
    public class AuditLogDto
    {
        public long Id { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string Controller { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public int StatusCode { get; set; }
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AuditLogQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? UserId { get; set; }
        public string? Controller { get; set; }
        public string? EntityId { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}
