using Microsoft.EntityFrameworkCore;
using Pickadate.Domain.Safety;
using Pickadate.Infrastructure.Persistence;

namespace Pickadate.Infrastructure.Repositories;

public class SafetyCheckRepository : ISafetyCheckRepository
{
    private readonly PickadateDbContext _db;
    public SafetyCheckRepository(PickadateDbContext db) => _db = db;

    public async Task AddAsync(SafetyCheck check, CancellationToken ct = default)
    {
        await _db.SafetyChecks.AddAsync(check, ct);
    }

    public Task<SafetyCheck?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.SafetyChecks.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<SafetyCheck?> GetByFriendTokenAsync(string friendToken, CancellationToken ct = default) =>
        _db.SafetyChecks.FirstOrDefaultAsync(s => s.FriendToken == friendToken, ct);

    public Task<SafetyCheck?> GetActiveForInvitationAsync(Guid invitationId, Guid userId, CancellationToken ct = default) =>
        _db.SafetyChecks
            .Where(s => s.InvitationId == invitationId && s.UserId == userId && s.ConfirmedAt == null)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<SafetyCheck>> GetDueForAlertAsync(DateTime nowUtc, CancellationToken ct = default) =>
        await _db.SafetyChecks
            .Where(s => s.ConfirmedAt == null && s.AlertedAt == null && s.ScheduledCheckInAt <= nowUtc)
            .ToListAsync(ct);
}
