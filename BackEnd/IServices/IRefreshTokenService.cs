using BackEnd.DTOs.Auth;

namespace BackEnd.IServices
{
    public interface IRefreshTokenService
    {
        Task<IssuedRefreshToken> IssueAsync(string userId);
        Task<RefreshRotationResult> RotateAsync(string? rawToken);
        Task RevokeSessionAsync(string? rawToken);
        Task RevokeAllAsync(string userId);
        Task<int> CleanupAsync();
    }
}