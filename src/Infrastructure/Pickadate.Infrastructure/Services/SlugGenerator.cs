using System.Security.Cryptography;
using Pickadate.Application.Contracts;

namespace Pickadate.Infrastructure.Services;

public class SlugGenerator : ISlugGenerator
{
    // Unambiguous characters only — no 0/o/1/l/i.
    private const string Alpha = "abcdefghjkmnpqrstuvwxyz";
    private const string Alphanum = "abcdefghjkmnpqrstuvwxyz23456789";

    public string Generate()
    {
        Span<char> buf = stackalloc char[7];
        buf[0] = Alpha[RandomNumberGenerator.GetInt32(Alpha.Length)];
        buf[1] = Alpha[RandomNumberGenerator.GetInt32(Alpha.Length)];
        buf[2] = '-';
        for (var i = 3; i < 7; i++)
            buf[i] = Alphanum[RandomNumberGenerator.GetInt32(Alphanum.Length)];
        return new string(buf);
    }
}
