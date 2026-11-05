namespace BackEnd.Middleware
{
    public class OriginCheckMiddleware
    {
        private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
        {
            "GET", "HEAD", "OPTIONS", "TRACE"
        };

        private readonly RequestDelegate _next;
        private readonly HashSet<string> _allowedOrigins;

        public OriginCheckMiddleware(RequestDelegate next, IConfiguration config)
        {
            _next = next;

            var origins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            _allowedOrigins = new HashSet<string>(origins.Select(o => o.TrimEnd('/')), StringComparer.OrdinalIgnoreCase);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!SafeMethods.Contains(context.Request.Method) && context.Request.Headers.TryGetValue("Origin", out var originValues))
            {
                var origin = originValues.ToString().TrimEnd('/');
                var selfOrigin = $"{context.Request.Scheme}://{context.Request.Host}";

                var allowed = string.Equals(origin, selfOrigin, StringComparison.OrdinalIgnoreCase) || _allowedOrigins.Contains(origin);
                if (!allowed)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new { message = "درخواست از مبدا نامعتبر ارسال شده است" });
                    return;
                }
            }

            await _next(context);
        }
    }
}