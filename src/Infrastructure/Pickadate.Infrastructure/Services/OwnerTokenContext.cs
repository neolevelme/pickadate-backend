using Microsoft.AspNetCore.Http;
using Pickadate.Application.Contracts;

namespace Pickadate.Infrastructure.Services;

public class OwnerTokenContext : IOwnerTokenContext
{
    private const string HeaderName = "X-Invitation-Owner-Token";

    private readonly IHttpContextAccessor _accessor;
    public OwnerTokenContext(IHttpContextAccessor accessor) => _accessor = accessor;

    public string? RawToken
    {
        get
        {
            var http = _accessor.HttpContext;
            if (http is null) return null;
            return http.Request.Headers.TryGetValue(HeaderName, out var v)
                ? v.ToString()
                : null;
        }
    }
}
