using Pickadate.BuildingBlocks.Domain;

namespace Pickadate.Domain.Auth;

public class VerificationCode : Entity
{
    // Verification codes live for 10 minutes per spec §1.
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);

    public Guid Id { get; private set; }
    public string Email { get; private set; } = null!;
    public string Code { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? UsedAt { get; private set; }

    private VerificationCode() { }

    public static VerificationCode Issue(string email, string code)
    {
        var now = DateTime.UtcNow;
        return new VerificationCode
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            Code = code,
            CreatedAt = now,
            ExpiresAt = now + Ttl
        };
    }

    public bool IsUsable(DateTime now) => UsedAt is null && now < ExpiresAt;

    public void MarkUsed() => UsedAt = DateTime.UtcNow;
}
