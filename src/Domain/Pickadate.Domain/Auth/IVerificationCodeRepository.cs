namespace Pickadate.Domain.Auth;

public interface IVerificationCodeRepository
{
    Task AddAsync(VerificationCode code, CancellationToken ct = default);

    /// <summary>Returns the most recently issued, still-usable code for this email, if any.</summary>
    Task<VerificationCode?> GetActiveAsync(string email, CancellationToken ct = default);

    /// <summary>Invalidates any existing unused codes so a re-request supersedes them.</summary>
    Task InvalidateOutstandingAsync(string email, CancellationToken ct = default);
}
