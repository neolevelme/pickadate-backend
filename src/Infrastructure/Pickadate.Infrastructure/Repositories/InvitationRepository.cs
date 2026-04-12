using Microsoft.EntityFrameworkCore;
using Pickadate.Domain.Invitations;
using Pickadate.Infrastructure.Persistence;

namespace Pickadate.Infrastructure.Repositories;

public class InvitationRepository : IInvitationRepository
{
    private readonly PickadateDbContext _db;
    public InvitationRepository(PickadateDbContext db) => _db = db;

    public Task<Invitation?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Invitations.FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<Invitation?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        _db.Invitations.FirstOrDefaultAsync(i => i.Slug == slug, ct);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default) =>
        _db.Invitations.AnyAsync(i => i.Slug == slug, ct);

    public async Task AddAsync(Invitation invitation, CancellationToken ct = default)
    {
        await _db.Invitations.AddAsync(invitation, ct);
    }

    public async Task<IReadOnlyList<Invitation>> ListForInitiatorAsync(Guid initiatorId, CancellationToken ct = default) =>
        await _db.Invitations
            .Where(i => i.InitiatorId == initiatorId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

    public Task<int> PurgeOlderThanAsync(DateTime cutoffUtc, CancellationToken ct = default) =>
        _db.Invitations
            .Where(i => i.MeetingAt < cutoffUtc)
            .ExecuteDeleteAsync(ct);
}
