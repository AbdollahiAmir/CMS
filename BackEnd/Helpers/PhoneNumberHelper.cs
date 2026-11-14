using System.Text;
using System.Text.RegularExpressions;

namespace BackEnd.Helpers
{
    public static partial class PhoneNumberHelper
    {
        [GeneratedRegex(@"^09\d{9}$")]
        private static partial Regex MobilePattern();

        public static string? Normalize(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            var builder = new StringBuilder();

            foreach (var ch in input.Trim())
            {
                if (ch >= '۰' && ch <= '۹')
                {
                    builder.Append((char)('0' + (ch - '۰')));
                }
                else if (ch >= '٠' && ch <= '٩')
                {
                    builder.Append((char)('0' + (ch - '٠')));
                }
                else if ((ch >= '0' && ch <= '9') || ch == '+')
                {
                    builder.Append(ch);
                }
                else if (ch is ' ' or '-' or '(' or ')')
                {
                    continue;
                }
                else
                {
                    return null;
                }
            }

            var value = builder.ToString();

            if (value.StartsWith("+98"))
            {
                value = "0" + value[3..];
            }
            else if (value.StartsWith("0098"))
            {
                value = "0" + value[4..];
            }
            else if (value.StartsWith("98") && value.Length == 12)
            {
                value = "0" + value[2..];
            }
            else if (value.Length == 10 && value[0] == '9')
            {
                value = "0" + value;
            }

            return MobilePattern().IsMatch(value) ? value : null;
        }
    }
}
