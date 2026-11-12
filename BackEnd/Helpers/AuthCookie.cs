namespace BackEnd.Helpers
{
    public static class AuthCookie
    {
        public const string Name = "access_token";
        public const string RefreshName = "refresh_token";
        public const string RefreshPath = "/api/Auth";

        public static CookieOptions Create(IConfiguration config, DateTimeOffset? expires, string path = "/")
        {
            var sameSite = Enum.TryParse<SameSiteMode>(config["Auth:CookieSameSite"], true, out var parsed)
                ? parsed
                : SameSiteMode.None;

            return new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = sameSite,
                Path = path,
                Expires = expires
            };
        }
    }
}