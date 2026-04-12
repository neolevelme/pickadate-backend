using Pickadate.Domain.Users;

namespace Pickadate.Application.Contracts;

public interface IJwtTokenService
{
    /// <summary>Issues a signed JWT for the given user. Returns the token and its absolute expiry.</summary>
    (string Token, DateTime ExpiresAt) Issue(User user);
}
