using BackEnd.IServices;
using Ganss.Xss;

namespace BackEnd.Services
{
    public class HtmlContentSanitizer : IHtmlContentSanitizer
    {
        private readonly HtmlSanitizer _sanitizer;

        public HtmlContentSanitizer()
        {
            _sanitizer = new HtmlSanitizer();

            _sanitizer.AllowedTags.Clear();
            _sanitizer.AllowedTags.UnionWith([
                "p", "br",
                "h1", "h2", "h3", "h4", "h5", "h6",
                "strong", "b", "em", "i", "u", "s",
                "blockquote",
                "ul", "ol", "li",
                "a", "img",
                "figure", "figcaption",
                "table", "thead", "tbody", "tfoot",
                "tr", "th", "td",
                "hr", "pre", "code",
                "span", "div"
            ]);

            _sanitizer.AllowedAttributes.Clear();
            _sanitizer.AllowedAttributes.UnionWith([
                "href", "src",
                "alt", "title",
                "width", "height",
                "class",
                "colspan", "rowspan"
            ]);

            _sanitizer.AllowedSchemes.Clear();
            _sanitizer.AllowedSchemes.UnionWith([
                "http", "https", "mailto"
            ]);

            _sanitizer.AllowedCssProperties.Clear();
        }

        public string Sanitize(string? html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return string.Empty;
            }

            return _sanitizer.Sanitize(html);
        }
    }
}