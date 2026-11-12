namespace BackEnd.DTOs.Auth
{
    public class AuthResult
    {
        public bool IsSuccess { get; set; }
        public string? Token { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshExpiresAt { get; set; }
        public AuthUserDto? User { get; set; }
        public IEnumerable<string> Errors { get; set; } = [];
    }
}