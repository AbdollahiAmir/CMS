namespace BackEnd.IServices
{
    public interface IIntegrationSettingsService
    {
        Task<T> GetAsync<T>(string key) where T : class, new();
        Task SaveAsync<T>(string key, T value, string? userId) where T : class;
    }
}
