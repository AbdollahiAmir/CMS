using BackEnd.DTOs.Auth;

namespace BackEnd.IServices
{
    public interface IAuthService
    {
        Task<AuthResult> RegisterAsync(RegisterDto dto);
        Task<AuthResult> LoginAsync(LoginDto dto);
        Task<AuthResult> RefreshAsync(string? refreshToken);
        Task LogoutAsync(string? refreshToken);
        Task<AuthResult> ChangePasswordAsync(string? userId, ChangePasswordDto dto);
    }
}