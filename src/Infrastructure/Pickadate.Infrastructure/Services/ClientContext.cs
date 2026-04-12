using Microsoft.AspNetCore.Http;
using Pickadate.Application.Contracts;

namespace Pickadate.Infrastructure.Services;

public class ClientContext : IClientContext
{
    private readonly IHttpContextAccessor _accessor;
    public ClientContext(IHttpContextAccessor accessor) => _accessor = accessor;

    public string Ip
    {
        get
        {
            var ctx = _accessor.HttpContext;
            if (ctx is null) return "unknown";

            // Honour X-Forwarded-For if the proxy set it. Take the left-most
            // entry — that's the original client; everything after is proxies.
            if (ctx.Request.Headers.TryGetValue("X-Forwarded-For", out var fwd))
            {
                var first = fwd.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .FirstOrDefault();
                if (!string.IsNullOrEmpty(first)) return first;
            }

            return ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}
