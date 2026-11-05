namespace BackEnd.Helpers
{
    public static class AuthCookie
    {
        public const string Name = "access_token";

        public static CookieOptions Create(IConfiguration config, DateTimeOffset? expires)
        {
            var sameSite = Enum.TryParse<SameSiteMode>(config["Auth:CookieSameSite"], true, out var parsed)
                ? parsed
                : SameSiteMode.None;

            return new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = sameSite,
                Path = "/",
                Expires = expires
            };
        }
    }
}