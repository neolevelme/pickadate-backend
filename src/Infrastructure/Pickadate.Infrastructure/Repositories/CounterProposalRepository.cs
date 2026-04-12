using Microsoft.EntityFrameworkCore;
using Pickadate.Domain.Invitations;
using Pickadate.Infrastructure.Persistence;

namespace Pickadate.Infrastructure.Repositories;

public class CounterProposalRepository : ICounterProposalRepository
{
    private readonly PickadateDbContext _db;
    public CounterProposalRepository(PickadateDbContext db) => _db = db;

    public async Task AddAsync(CounterProposal counterProposal, CancellationToken ct = default)
    {
        await _db.CounterProposals.AddAsync(counterProposal, ct);
    }

    public Task<CounterProposal?> GetLatestForInvitationAsync(Guid invitationId, CancellationToken ct = default) =>
        _db.CounterProposals
            .Where(c => c.InvitationId == invitationId)
            .OrderByDescending(c => c.Round)
            .FirstOrDefaultAsync(ct);
}
