namespace BackEnd.IServices
{
    public interface IHtmlContentSanitizer
    {
        string Sanitize(string? html);
    }
}