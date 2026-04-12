using System.Security.Cryptography;
using Pickadate.Application.Contracts;

namespace Pickadate.Infrastructure.Services;

public class VerificationCodeGenerator : IVerificationCodeGenerator
{
    public string Generate()
    {
        // 6-digit code from a cryptographically strong RNG. Zero-padded so
        // every code is exactly 6 characters long (matches auto-fill hint).
        var n = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return n.ToString("D6");
    }
}
