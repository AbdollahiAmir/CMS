namespace BackEnd.DTOs.Auth
{
    public class AuthUserDto
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string[] Roles { get; set; } = [];
        public string[] Permissions { get; set; } = [];
    }
}