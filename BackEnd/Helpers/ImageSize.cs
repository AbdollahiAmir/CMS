namespace BackEnd.Helpers
{
    public static class ImageSize
    {
        public static bool IsThumbnail(string? size)
        {
            return string.Equals(size, "thumb", StringComparison.OrdinalIgnoreCase);
        }
    }
}