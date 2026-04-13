using System.Security.Cryptography;
using System.Text;
using Pickadate.Application.Contracts;

namespace Pickadate.Infrastructure.Services;

/// <summary>
/// Crockford base32 owner token generator. 10 random bytes → 16 base32 chars
/// → grouped as four blocks of four with dashes (e.g. `XK4P-2Q9V-R7MN-3BDF`).
/// 80 bits of entropy is enough — these are non-guessable bearer tokens, not
/// cryptographic keys, and the grouped format makes them easy to copy/paste
/// or write down on the back of a napkin.
/// </summary>
public class OwnerTokenGenerator : IOwnerTokenGenerator
{
    // Crockford base32 alphabet: no 0/O/1/I/L confusion, no U.
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    public OwnerToken Generate()
    {
        Span<byte> bytes = stackalloc byte[10];
        RandomNumberGenerator.Fill(bytes);

        // 10 bytes = 80 bits → 16 base32 chars (5 bits each).
        Span<char> chars = stackalloc char[16];
        EncodeBase32(bytes, chars);

        // Insert dashes after every 4 chars: XXXX-XXXX-XXXX-XXXX
        var formatted = $"{new string(chars[..4])}-{new string(chars[4..8])}-{new string(chars[8..12])}-{new string(chars[12..16])}";

        return new OwnerToken(Raw: formatted, Hash: HashNormalised(chars));
    }

    public string Hash(string rawToken) => HashNormalised(Normalise(rawToken));

    private static string HashNormalised(ReadOnlySpan<char> normalised)
    {
        var bytes = Encoding.ASCII.GetBytes(normalised.ToArray());
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    /// <summary>Strip whitespace + dashes, uppercase, fix common typos (O→0, I→1, L→1).</summary>
    private static char[] Normalise(string input)
    {
        var buf = new List<char>(input.Length);
        foreach (var raw in input)
        {
            if (raw is ' ' or '-' or '\t' or '\r' or '\n') continue;
            var c = char.ToUpperInvariant(raw);
            c = c switch
            {
                'O' => '0',
                'I' or 'L' => '1',
                _ => c
            };
            buf.Add(c);
        }
        return buf.ToArray();
    }

    private static void EncodeBase32(ReadOnlySpan<byte> bytes, Span<char> output)
    {
        // 10 bytes (80 bits) → 16 chars (5 bits each). We pull 5 bits at a
        // time from the bit stream, MSB first.
        int bitBuffer = 0;
        int bitsInBuffer = 0;
        int outIdx = 0;

        foreach (var b in bytes)
        {
            bitBuffer = (bitBuffer << 8) | b;
            bitsInBuffer += 8;
            while (bitsInBuffer >= 5)
            {
                bitsInBuffer -= 5;
                var index = (bitBuffer >> bitsInBuffer) & 0b11111;
                output[outIdx++] = Alphabet[index];
            }
        }

        // 80 % 5 == 0, so no leftover bits — assert by construction.
    }
}
