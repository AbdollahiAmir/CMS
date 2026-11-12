namespace BackEnd.Settings
{
    public class SiteOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Language { get; set; } = "fa";
        public string PostPathTemplate { get; set; } = "/blog/{slug}";
        public string PagePathTemplate { get; set; } = "/pages/{slug}";
        public string ProductPathTemplate { get; set; } = "/products/{slug}";
    }
}
