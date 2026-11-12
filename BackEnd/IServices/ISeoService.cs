namespace BackEnd.IServices
{
    public interface ISeoService
    {
        Task<string?> BuildSitemapAsync();
        Task<string?> BuildRssAsync();
    }
}
