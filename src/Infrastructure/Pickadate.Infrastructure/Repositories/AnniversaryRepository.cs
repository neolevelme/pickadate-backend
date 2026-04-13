using Microsoft.EntityFrameworkCore;
using Pickadate.Domain.Anniversaries;
using Pickadate.Infrastructure.Persistence;

namespace Pickadate.Infrastructure.Repositories;

public class AnniversaryRepository : IAnniversaryRepository
{
    private readonly PickadateDbContext _db;
    public AnniversaryRepository(PickadateDbContext db) => _db = db;

    public async Task AddAsync(Anniversary anniversary, CancellationToken ct = default)
    {
        await _db.Anniversaries.AddAsync(anniversary, ct);
    }

    public Task<bool> ExistsForPairAsync(Guid userOneId, Guid userTwoId, CancellationToken ct = default)
    {
        // Canonicalize the query so it matches the stored order.
        var (a, b) = userOneId.CompareTo(userTwoId) < 0 ? (userOneId, userTwoId) : (userTwoId, userOneId);
        return _db.Anniversaries.AnyAsync(x => x.UserAId == a && x.UserBId == b, ct);
    }

    public async Task<IReadOnlyList<Anniversary>> GetDueOnAsync(int month, int day, CancellationToken ct = default) =>
        await _db.Anniversaries
            .Where(a => a.FirstDateAt.Month == month && a.FirstDateAt.Day == day)
            .ToListAsync(ct);
}
