using BackEnd.DTOs.Account;
using BackEnd.DTOs.Auth;
using BackEnd.Models;

namespace BackEnd.IServices
{
    public interface IAccountService
    {
        Task<ProfileDto?> GetProfileAsync(string userId);
        Task<ProfileDto?> UpdateProfileAsync(string userId, UpdateProfileDto dto);
        Task<AuthResult> RequestEmailChangeAsync(string userId, string email);
        Task SendEmailConfirmationAsync(AppUser user);
        Task<AuthResult> ResendEmailConfirmationAsync(string userId);
        Task<AuthResult> ConfirmEmailAsync(ConfirmEmailDto dto);
        Task<AuthResult> RequestPhoneCodeAsync(string userId, string phoneNumber);
        Task<AuthResult> VerifyPhoneAsync(string userId, VerifyPhoneDto dto);
        Task ForgotPasswordAsync(string email);
        Task<AuthResult> ResetPasswordAsync(ResetPasswordDto dto);
    }
}
