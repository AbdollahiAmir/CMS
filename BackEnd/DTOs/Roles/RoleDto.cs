namespace BackEnd.DTOs.Roles
{
    public class RoleDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsSystem { get; set; }
        public string[] Permissions { get; set; } = [];
    }
}