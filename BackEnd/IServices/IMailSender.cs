namespace BackEnd.IServices
{
    public interface IMailSender
    {
        Task<bool> IsConfiguredAsync();

        Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
    }
}
