using BackEnd.Data;
using BackEnd.IServices;
using BackEnd.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Xml.Linq;

namespace BackEnd.Services
{
    public class SeoService : ISeoService
    {
        private const int MaxSitemapUrls = 50_000;
        private const int RssItemCount = 50;
        private const string SlugToken = "{slug}";

        private static readonly XNamespace SitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";

        private readonly AppDbContext _context;
        private readonly SiteOptions _options;

        public SeoService(AppDbContext context, IOptions<SiteOptions> options)
        {
            _context = context;
            _options = options.Value;
        }

        private string? GetBaseUrl()
        {
            var value = _options.BaseUrl?.Trim().TrimEnd('/');

            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return null;
            }

            return value;
        }

        private static string BuildUrl(string baseUrl, string template, string slug)
        {
            var path = template.Contains(SlugToken, StringComparison.Ordinal)
                ? template.Replace(SlugToken, Uri.EscapeDataString(slug), StringComparison.Ordinal)
                : template;

            if (!path.StartsWith('/'))
            {
                path = "/" + path;
            }

            return baseUrl + path;
        }

        private static DateTime AsUtc(DateTime value)
        {
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        private static string FormatSitemapDate(DateTime value)
        {
            return AsUtc(value).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        }

        private static string FormatRssDate(DateTime value)
        {
            return new DateTimeOffset(AsUtc(value)).ToString("r", CultureInfo.InvariantCulture);
        }

        private static string ToXml(XDocument document)
        {
            return $"{document.Declaration}\n{document.Root}";
        }

        public async Task<string?> BuildSitemapAsync()
        {
            var baseUrl = GetBaseUrl();

            if (baseUrl is null)
            {
                return null;
            }

            var entries = new List<(string Location, DateTime? LastModified)>
            {
                (baseUrl + "/", null)
            };

            var posts = await _context.Posts
                .AsNoTracking()
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.PublishedAt)
                .ThenByDescending(p => p.Id)
                .Take(MaxSitemapUrls - entries.Count)
                .Select(p => new { p.Slug, LastModified = p.UpdatedAt ?? p.PublishedAt })
                .ToListAsync();

            entries.AddRange(posts.Select(p => (BuildUrl(baseUrl, _options.PostPathTemplate, p.Slug), p.LastModified)));

            var pages = await _context.ContentPages
                .AsNoTracking()
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.PublishedAt)
                .ThenByDescending(p => p.Id)
                .Take(Math.Max(0, MaxSitemapUrls - entries.Count))
                .Select(p => new { p.Slug, LastModified = p.UpdatedAt ?? p.PublishedAt })
                .ToListAsync();

            entries.AddRange(pages.Select(p => (BuildUrl(baseUrl, _options.PagePathTemplate, p.Slug), p.LastModified)));

            var products = await _context.Products
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .ThenByDescending(p => p.Id)
                .Take(Math.Max(0, MaxSitemapUrls - entries.Count))
                .Select(p => new { p.Slug, LastModified = (DateTime?)(p.UpdatedAt ?? p.CreatedAt) })
                .ToListAsync();

            entries.AddRange(products.Select(p => (BuildUrl(baseUrl, _options.ProductPathTemplate, p.Slug), p.LastModified)));

            var document = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(SitemapNamespace + "urlset",
                    entries.Select(entry => new XElement(SitemapNamespace + "url",
                        new XElement(SitemapNamespace + "loc", entry.Location),
                        entry.LastModified.HasValue
                            ? new XElement(SitemapNamespace + "lastmod", FormatSitemapDate(entry.LastModified.Value))
                            : null))));

            return ToXml(document);
        }

        public async Task<string?> BuildRssAsync()
        {
            var baseUrl = GetBaseUrl();

            if (baseUrl is null)
            {
                return null;
            }

            var posts = await _context.Posts
                .AsNoTracking()
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.PublishedAt)
                .ThenByDescending(p => p.Id)
                .Take(RssItemCount)
                .Select(p => new { p.Title, p.Slug, p.Excerpt, p.PublishedAt })
                .ToListAsync();

            var title = string.IsNullOrWhiteSpace(_options.Title)
                ? new Uri(baseUrl).Host
                : _options.Title.Trim();

            var description = string.IsNullOrWhiteSpace(_options.Description)
                ? title
                : _options.Description.Trim();

            var channel = new XElement("channel",
                new XElement("title", title),
                new XElement("link", baseUrl),
                new XElement("description", description),
                new XElement("language", string.IsNullOrWhiteSpace(_options.Language) ? "fa" : _options.Language.Trim()));

            var newest = posts.FirstOrDefault(p => p.PublishedAt.HasValue)?.PublishedAt;

            if (newest.HasValue)
            {
                channel.Add(new XElement("lastBuildDate", FormatRssDate(newest.Value)));
            }

            foreach (var post in posts)
            {
                var link = BuildUrl(baseUrl, _options.PostPathTemplate, post.Slug);

                var item = new XElement("item",
                    new XElement("title", post.Title),
                    new XElement("link", link),
                    new XElement("guid", new XAttribute("isPermaLink", "true"), link));

                if (post.PublishedAt.HasValue)
                {
                    item.Add(new XElement("pubDate", FormatRssDate(post.PublishedAt.Value)));
                }

                if (!string.IsNullOrWhiteSpace(post.Excerpt))
                {
                    item.Add(new XElement("description", post.Excerpt));
                }

                channel.Add(item);
            }

            var document = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("rss", new XAttribute("version", "2.0"), channel));

            return ToXml(document);
        }
    }
}
