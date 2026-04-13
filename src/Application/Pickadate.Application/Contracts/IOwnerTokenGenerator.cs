namespace Pickadate.Application.Contracts;

public record OwnerToken(string Raw, string Hash);

public interface IOwnerTokenGenerator
{
    /// <summary>
    /// Generates a fresh raw token (human-friendly Crockford base32, grouped
    /// with dashes) and its SHA-256 hash. The raw value is shown to the user
    /// once and stored in browser localStorage; only the hash is persisted
    /// server-side.
    /// </summary>
    OwnerToken Generate();

    /// <summary>
    /// Hashes a user-provided token for comparison against stored hashes.
    /// Normalises whitespace, dashes, and case so users can paste the
    /// recovery code in any reasonable format.
    /// </summary>
    string Hash(string rawToken);
}
