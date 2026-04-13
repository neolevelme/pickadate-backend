namespace Pickadate.Application.Contracts;

/// <summary>
/// Reads the bearer owner token a client sends along with a mutation
/// request — header `X-Invitation-Owner-Token`. Null when the request
/// doesn't carry one (the user might be authenticated instead).
/// </summary>
public interface IOwnerTokenContext
{
    string? RawToken { get; }
}
