using Microsoft.EntityFrameworkCore;
using Pickadate.Domain.AntiAbuse;
using Pickadate.Infrastructure.Persistence;

namespace Pickadate.Infrastructure.Repositories;

public class DeclineRecordRepository : IDeclineRecordRepository
{
    private readonly PickadateDbContext _db;
    public DeclineRecordRepository(PickadateDbContext db) => _db = db;

    public async Task AddAsync(DeclineRecord record, CancellationToken ct = default)
    {
        await _db.DeclineRecords.AddAsync(record, ct);
    }

    public Task<int> CountInLast24hAsync(string ip, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddHours(-24);
        return _db.DeclineRecords.CountAsync(r => r.Ip == ip && r.CreatedAt >= cutoff, ct);
    }
}
