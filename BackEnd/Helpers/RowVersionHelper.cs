using Microsoft.EntityFrameworkCore;

namespace BackEnd.Helpers
{
    public static class RowVersionHelper
    {
        public static string Encode(byte[] rowVersion)
        {
            return Convert.ToBase64String(rowVersion);
        }

        public static byte[] Decode(string? rowVersion)
        {
            if (string.IsNullOrWhiteSpace(rowVersion))
            {
                throw new ArgumentException("نسخه اطلاعات الزامی است");
            }

            var value = rowVersion.Trim();

            if (value.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("نسخه اطلاعات معتبر نیست");
            }

            if (value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"'))
            {
                value = value[1..^1];
            }

            byte[] bytes;

            try
            {
                bytes = Convert.FromBase64String(value);
            }
            catch (FormatException)
            {
                throw new ArgumentException("نسخه اطلاعات معتبر نیست");
            }

            if (bytes.Length != 8)
            {
                throw new ArgumentException("نسخه اطلاعات معتبر نیست");
            }

            return bytes;
        }

        public static void SetOriginalValue<TEntity>(DbContext context, TEntity entity, string? rowVersion) where TEntity : class
        {
            context.Entry(entity).Property<byte[]>("RowVersion").OriginalValue = Decode(rowVersion);
        }

        public static string ToETag(string rowVersion)
        {
            return $"\"{rowVersion}\"";
        }
    }
}