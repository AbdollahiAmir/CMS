namespace BackEnd.Helpers
{
    public static class UrlValidator
    {
        public static string? Normalize(string? value, string fieldName, bool allowRelative = true)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var url = value.Trim();

            if (url.Any(char.IsControl) || url.Any(char.IsWhiteSpace))
            {
                throw new ArgumentException($"{fieldName} معتبر نیست");
            }

            if (allowRelative && url.StartsWith('/') && !url.StartsWith("//", StringComparison.Ordinal) && !url.StartsWith("/\\", StringComparison.Ordinal))
            {
                return url;
            }

            if (Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                && !string.IsNullOrEmpty(uri.Host))
            {
                return url;
            }

            throw new ArgumentException($"{fieldName} باید آدرس کامل http یا https یا مسیر نسبی با / باشد");
        }
    }
}
