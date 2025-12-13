using System.Security.Claims;
using Ping.Services.Moderation;

namespace Ping.Middleware;

public class BanningMiddleware(IBanningService banningService, ILogger<BanningMiddleware> logger) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // 1. Check IP Ban
        var ip = GetClientIp(context);
        
        // 2. Check User Ban (if authenticated)
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        var (isBanned, reason) = await banningService.CheckBanAsync(ip, userId);

        if (isBanned)
        {
            logger.LogWarning("Blocked request from banned source. IP: {Ip}, User: {UserId}. Reason: {Reason}", ip, userId ?? "Anonymous", reason);
            
            context.Response.StatusCode = 403; // Forbidden
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { 
                error = "You have been banned from accessing this service.",
                reason = reason ?? "No reason specified."
            });
            return;
        }

        await next(context);
    }
    
    private static string GetClientIp(HttpContext context)
    {
        // Simple extraction matching RateLimitMiddleware except we don't need complex logic?
        // Actually we should reuse the same logic if possible or copy it.
        // RateLimitMiddleware uses X-Forwarded-For
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out Microsoft.Extensions.Primitives.StringValues forwardedFor))
        {
            var ip = forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(ip))
                return ip;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

