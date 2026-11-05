namespace BackEnd.DTOs.Roles
{
    public class RoleResult
    {
        public bool IsSuccess { get; set; }
        public bool NotFound { get; set; }
        public bool Forbidden { get; set; }
        public IEnumerable<string> Errors { get; set; } = [];
        public RoleDto? Role { get; set; }
    }
}