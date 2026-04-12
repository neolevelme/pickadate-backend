using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Pickadate.Application.Contracts;

namespace Pickadate.Infrastructure.Services;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    public Guid? UserId
    {
        get
        {
            var sub = _accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? _accessor.HttpContext?.User.FindFirstValue("sub");
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public Guid RequireUserId() => UserId
        ?? throw new UnauthorizedAccessException("Authentication required.");
}
