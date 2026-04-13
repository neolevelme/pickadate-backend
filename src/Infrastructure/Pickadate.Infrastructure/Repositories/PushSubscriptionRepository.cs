using Microsoft.EntityFrameworkCore;
using Pickadate.Domain.Notifications;
using Pickadate.Infrastructure.Persistence;

namespace Pickadate.Infrastructure.Repositories;

public class PushSubscriptionRepository : IPushSubscriptionRepository
{
    private readonly PickadateDbContext _db;
    public PushSubscriptionRepository(PickadateDbContext db) => _db = db;

    public async Task AddAsync(PushSubscription subscription, CancellationToken ct = default)
    {
        await _db.PushSubscriptions.AddAsync(subscription, ct);
    }

    public Task<PushSubscription?> GetByEndpointAsync(string endpoint, CancellationToken ct = default) =>
        _db.PushSubscriptions.FirstOrDefaultAsync(p => p.Endpoint == endpoint, ct);

    public async Task<IReadOnlyList<PushSubscription>> GetForUserAsync(Guid userId, CancellationToken ct = default) =>
        await _db.PushSubscriptions.Where(p => p.UserId == userId).ToListAsync(ct);

    public async Task<IReadOnlyList<PushSubscription>> GetForUsersAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken ct = default) =>
        await _db.PushSubscriptions.Where(p => userIds.Contains(p.UserId)).ToListAsync(ct);

    public Task DeleteByEndpointAsync(string endpoint, CancellationToken ct = default) =>
        _db.PushSubscriptions.Where(p => p.Endpoint == endpoint).ExecuteDeleteAsync(ct);
}
