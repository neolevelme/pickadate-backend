using Microsoft.EntityFrameworkCore;
using Pickadate.Domain.Auth;
using Pickadate.Infrastructure.Persistence;

namespace Pickadate.Infrastructure.Repositories;

public class VerificationCodeRepository : IVerificationCodeRepository
{
    private readonly PickadateDbContext _db;
    public VerificationCodeRepository(PickadateDbContext db) => _db = db;

    public async Task AddAsync(VerificationCode code, CancellationToken ct = default)
    {
        await _db.VerificationCodes.AddAsync(code, ct);
    }

    public async Task<VerificationCode?> GetActiveAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;
        return await _db.VerificationCodes
            .Where(v => v.Email == normalized && v.UsedAt == null && v.ExpiresAt > now)
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task InvalidateOutstandingAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;
        var outstanding = await _db.VerificationCodes
            .Where(v => v.Email == normalized && v.UsedAt == null && v.ExpiresAt > now)
            .ToListAsync(ct);
        foreach (var c in outstanding) c.MarkUsed();
    }
}
