using System.Security.Claims;
using BackEnd.Data;
using BackEnd.Models;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace BackEnd.Filters
{
    public class AuditLogFilter : IAsyncActionFilter
    {
        private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
        {
            "GET", "HEAD", "OPTIONS", "TRACE"
        };

        private static readonly HashSet<string> ExcludedControllers = new(StringComparer.Ordinal)
        {
            "Auth",
            "AuditLogs"
        };

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AuditLogFilter> _logger;

        public AuditLogFilter(IServiceScopeFactory scopeFactory, ILogger<AuditLogFilter> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        private static string Truncate(string? value, int max)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Length <= max ? value : value[..max];
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var executed = await next();

            var request = context.HttpContext.Request;

            if (SafeMethods.Contains(request.Method))
            {
                return;
            }

            if (context.ActionDescriptor is not ControllerActionDescriptor descriptor
                || ExcludedControllers.Contains(descriptor.ControllerName))
            {
                return;
            }

            var user = context.HttpContext.User;

            if (user.Identity?.IsAuthenticated != true)
            {
                return;
            }

            if (executed.Exception is not null && !executed.ExceptionHandled)
            {
                return;
            }

            var statusCode = executed.Result is IStatusCodeActionResult { StatusCode: not null } statusResult
                ? statusResult.StatusCode.Value
                : 200;

            if (statusCode is < 200 or >= 300)
            {
                return;
            }

            var entry = new AuditLog
            {
                UserId = Truncate(user.FindFirstValue(ClaimTypes.NameIdentifier), 450),
                UserName = Truncate(user.Identity?.Name, 256),
                Controller = Truncate(descriptor.ControllerName, 100),
                Action = Truncate(descriptor.ActionName, 100),
                Method = Truncate(request.Method, 10),
                Path = Truncate(request.Path.Value, 500),
                EntityId = context.RouteData.Values.TryGetValue("id", out var id)
                    ? Truncate(Convert.ToString(id), 50)
                    : null,
                StatusCode = statusCode,
                IpAddress = Truncate(context.HttpContext.Connection.RemoteIpAddress?.ToString(), 64),
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.AuditLogs.Add(entry);
                await db.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write audit log for {Method} {Path}", entry.Method, entry.Path);
            }
        }
    }
}
