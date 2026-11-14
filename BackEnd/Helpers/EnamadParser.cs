using System.Text.RegularExpressions;

namespace BackEnd.Helpers
{
    public static partial class EnamadParser
    {
        [GeneratedRegex(@"[?&;]id=(\d{1,12})", RegexOptions.IgnoreCase)]
        private static partial Regex IdPattern();

        [GeneratedRegex(@"(?:[?&;]|\s)code=[""']?([A-Za-z0-9]{6,64})", RegexOptions.IgnoreCase)]
        private static partial Regex CodePattern();

        [GeneratedRegex(@"name=[""']enamad[""']\s+content=[""']([A-Za-z0-9]{6,64})", RegexOptions.IgnoreCase)]
        private static partial Regex MetaPattern();

        [GeneratedRegex(@"^[A-Za-z0-9]{6,64}$")]
        private static partial Regex PlainCodePattern();

        public static (string? Id, string? Code) Parse(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return (null, null);
            }

            var text = input.Trim();

            if (PlainCodePattern().IsMatch(text))
            {
                return (null, text);
            }

            var id = IdPattern().Match(text);
            var code = CodePattern().Match(text);

            if (!code.Success)
            {
                code = MetaPattern().Match(text);
            }

            return (
                id.Success ? id.Groups[1].Value : null,
                code.Success ? code.Groups[1].Value : null);
        }
    }
}
