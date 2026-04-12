namespace Pickadate.Application.Contracts;

public interface IClientContext
{
    /// <summary>Best-effort client IP (honours X-Forwarded-For, falls back to the socket address).</summary>
    string Ip { get; }
}
