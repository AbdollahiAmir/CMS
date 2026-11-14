namespace BackEnd.IServices
{
    public interface IMailSender
    {
        Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
    }
}
