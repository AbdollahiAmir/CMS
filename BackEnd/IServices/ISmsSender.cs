namespace BackEnd.IServices
{
    public interface ISmsSender
    {
        Task<bool> IsConfiguredAsync();

        Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
    }
}
