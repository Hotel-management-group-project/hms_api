
using HMS.API.Data;
using HMS.API.Models;
using System.Security.Claims;

namespace HMS.API.Middleware
{
    public class AuditLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceScopeFactory _scopeFactory;

        // Prefixes that produce no audit value and would flood the table
        private static readonly string[] SkipPrefixes =
        [
            "/swagger", "/health", "/favicon.ico", "/_", "/openapi"
        ];

        public AuditLoggingMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory)
        {
            _next = next;
            _scopeFactory = scopeFactory;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);

            var path = context.Request.Path.Value ?? string.Empty;

            if (SkipPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                return;

            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Only log authenticated requests to avoid noise from public probing
            if (string.IsNullOrEmpty(userId))
                return;

            var method = context.Request.Method;
            var statusCode = context.Response.StatusCode;
            var ip = context.Connection.RemoteIpAddress?.ToString();

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                db.AuditLogs.Add(new AuditLog
                {
                    UserId = userId,
                    Action = $"{method} {path}",
                    EntityType = "HttpRequest",
                    IpAddress = ip,
                    Details = $"HTTP {statusCode}",
                    CreatedAt = DateTime.UtcNow
                });

                await db.SaveChangesAsync();
            }
            catch
            {
                // Audit logging must never break the request pipeline
            }
        }
    }
}
