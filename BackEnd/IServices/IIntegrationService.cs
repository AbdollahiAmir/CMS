using BackEnd.DTOs.Integrations;

namespace BackEnd.IServices
{
    public interface IIntegrationService
    {
        Task<EmailIntegrationDto> GetEmailAsync();
        Task<EmailIntegrationDto> UpdateEmailAsync(UpdateEmailIntegrationDto dto, string? userId);
        Task SendTestEmailAsync(string to);
        Task<SmsIntegrationDto> GetSmsAsync();
        Task<SmsIntegrationDto> UpdateSmsAsync(UpdateSmsIntegrationDto dto, string? userId);
        Task SendTestSmsAsync(string phoneNumber);
        Task<PaymentIntegrationDto> GetPaymentAsync();
        Task<PaymentIntegrationDto> UpdatePaymentAsync(UpdatePaymentIntegrationDto dto, string? userId);
        Task<EnamadIntegrationDto> GetEnamadAsync();
        Task<EnamadIntegrationDto> UpdateEnamadAsync(UpdateEnamadIntegrationDto dto, string? userId);
        Task<PublicTrustBadgesDto> GetPublicTrustBadgesAsync();
    }
}
