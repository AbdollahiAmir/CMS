namespace BackEnd.DTOs.Auth
{
    public record IssuedRefreshToken(string Token, DateTime ExpiresAt);

    public class RefreshRotationResult
    {
        public bool Succeeded { get; set; }
        public string? UserId { get; set; }
        public IssuedRefreshToken? Token { get; set; }

        public static RefreshRotationResult Failed()
        {
            return new RefreshRotationResult();
        }
    }
}