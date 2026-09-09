using AskMe.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace AskMe.Infrastructure.Services;

public class RequestContextService : IRequestContextService
{
    private readonly IHttpContextAccessor _accessor;

    public RequestContextService(IHttpContextAccessor accessor) => _accessor = accessor;

    public string IpAddress
    {
        get
        {
            var ctx = _accessor.HttpContext;
            if (ctx is null) return "unknown";

            // Different hosts/proxies (Render, Railway, Cloudflare, ...) set
            // slightly different headers - check the common ones in order.
            // Program.cs also runs UseForwardedHeaders, which normally makes
            // Connection.RemoteIpAddress correct on its own; these header
            // reads are a second layer in case that middleware's trusted-proxy
            // config doesn't match a given host's network setup exactly.
            foreach (var header in new[] { "X-Forwarded-For", "CF-Connecting-IP", "X-Real-IP", "True-Client-IP" })
            {
                if (ctx.Request.Headers.TryGetValue(header, out var value) && !string.IsNullOrWhiteSpace(value))
                {
                    var first = value.ToString().Split(',')[0].Trim();
                    if (!string.IsNullOrEmpty(first)) return first;
                }
            }

            return ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }

    public string? UserAgent => _accessor.HttpContext?.Request.Headers.UserAgent.ToString();
}
