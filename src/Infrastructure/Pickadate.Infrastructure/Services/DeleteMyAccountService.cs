using Microsoft.EntityFrameworkCore;
using Pickadate.Application.Users.Commands;
using Pickadate.Infrastructure.Persistence;

namespace Pickadate.Infrastructure.Services;

/// <summary>
/// Wide delete of every row tied to a single user, as required by
/// spec §12 GDPR right to erasure. Touches every entity the user owns
/// or participates in:
///
///   - invitations they initiated → deleted (counter-proposals cascade
///     conceptually; we delete them explicitly since there's no FK)
///   - invitations where they were the recipient → anonymised
///     (RecipientId → null) so the other party's record survives
///   - counter-proposals where they proposed → deleted
///   - push subscriptions → deleted
///   - safety checks → deleted
///   - anniversaries involving them → deleted (unfortunately both
///     halves lose the record; GDPR trumps sentiment)
///   - verification codes tied to their email → deleted
///   - the user row → deleted
/// </summary>
public class DeleteMyAccountService : IDeleteMyAccountService
{
    private readonly PickadateDbContext _db;

    public DeleteMyAccountService(PickadateDbContext db) => _db = db;

    public async Task ExecuteAsync(Guid userId, CancellationToken ct)
    {
        // Look up the user first so we can clear verification codes by email.
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return; // idempotent — already gone

        // All invitation ids the user initiated, so we can scope the counter-
        // proposal delete to rows they either initiated or proposed.
        var initiatedIds = await _db.Invitations
            .Where(i => i.InitiatorId == userId)
            .Select(i => i.Id)
            .ToListAsync(ct);

        // Counter proposals: either on one of the user's invitations, or
        // proposed by the user on someone else's invitation.
        await _db.CounterProposals
            .Where(c => c.ProposerId == userId || initiatedIds.Contains(c.InvitationId))
            .ExecuteDeleteAsync(ct);

        // Safety checks the user owns.
        await _db.SafetyChecks
            .Where(s => s.UserId == userId)
            .ExecuteDeleteAsync(ct);

        // Push subscriptions.
        await _db.PushSubscriptions
            .Where(p => p.UserId == userId)
            .ExecuteDeleteAsync(ct);

        // Anniversaries where the user is either half of the pair.
        await _db.Anniversaries
            .Where(a => a.UserAId == userId || a.UserBId == userId)
            .ExecuteDeleteAsync(ct);

        // The user's own invitations (and their tombstones).
        await _db.Invitations
            .Where(i => i.InitiatorId == userId)
            .ExecuteDeleteAsync(ct);

        // Invitations where they were the recipient: anonymise so the other
        // side's history stays intact but nothing points back at this account.
        await _db.Invitations
            .Where(i => i.RecipientId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.RecipientId, (Guid?)null), ct);

        // Verification codes tied to the email — should all be used/expired
        // anyway, but clean them up for completeness.
        var email = user.Email;
        await _db.VerificationCodes
            .Where(v => v.Email == email)
            .ExecuteDeleteAsync(ct);

        // Finally the user row.
        await _db.Users
            .Where(u => u.Id == userId)
            .ExecuteDeleteAsync(ct);
    }
}
