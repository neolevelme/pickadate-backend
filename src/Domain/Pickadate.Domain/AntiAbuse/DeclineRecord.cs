namespace Pickadate.Domain.AntiAbuse;

/// <summary>
/// Anti-abuse breadcrumb for anonymous declines (spec §13). Stores only
/// IP + timestamp — nothing user-identifying. Purged after 24h.
/// </summary>
public class DeclineRecord
{
    public Guid Id { get; private set; }
    public string Ip { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    private DeclineRecord() { }

    public static DeclineRecord Create(string ip) => new()
    {
        Id = Guid.NewGuid(),
        Ip = ip,
        CreatedAt = DateTime.UtcNow
    };
}

public interface IDeclineRecordRepository
{
    Task AddAsync(DeclineRecord record, CancellationToken ct = default);
    Task<int> CountInLast24hAsync(string ip, CancellationToken ct = default);
}
