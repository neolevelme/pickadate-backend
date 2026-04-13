using System.Security.Cryptography;
using Pickadate.Application.Contracts;

namespace Pickadate.Infrastructure.Services;

/// <summary>
/// 32-char URL-safe token for friend safety-check links. Longer than the
/// invitation slug because the friend link is a bearer capability — anyone
/// with the URL can see the meeting, so it must be impractical to guess.
/// </summary>
public class SafetyTokenGenerator : ISafetyTokenGenerator
{
    public string Generate()
    {
        // 24 bytes -> 32 characters in base64url, no padding.
        Span<byte> bytes = stackalloc byte[24];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
