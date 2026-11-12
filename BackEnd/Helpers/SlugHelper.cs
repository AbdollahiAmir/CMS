using System.Text;

namespace BackEnd.Helpers
{
    public static class SlugHelper
    {

        // جنریت کردن اسلاگ از متن
        public static string Generate(string value)
        {
            var builder = new StringBuilder();
            var lastWasDash = false;

            foreach (var ch in value.Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(ch))
                {
                    builder.Append(ch);
                    lastWasDash = false;
                }
                else if (!lastWasDash && builder.Length > 0)
                {
                    builder.Append('-');
                    lastWasDash = true;
                }
            }

            return builder.ToString().Trim('-');
        }

        public static async Task<string> ResolveAsync(string? requestedSlug, string name, Func<string, Task<bool>> existsAsync)
        {
            if (!string.IsNullOrWhiteSpace(requestedSlug))
            {
                var custom = Generate(requestedSlug);
                if (custom.Length == 0)
                {
                    throw new ArgumentException("اسلاگ نامعتبر است");
                }

                if (await existsAsync(custom))
                {
                    throw new ArgumentException("این اسلاگ قبلا استفاده شده است");
                }

                return custom;
            }

            var baseSlug = Generate(name);
            if (baseSlug.Length == 0)
            {
                throw new ArgumentException("از روی این نام نمیتوان اسلاگ ساخت، یک اسلاگ وارد کنید");
            }

            var slug = baseSlug;
            var counter = 2;

            while (await existsAsync(slug))
            {
                slug = $"{baseSlug}-{counter}";
                counter++;
            }

            return slug;
        }
    }
}