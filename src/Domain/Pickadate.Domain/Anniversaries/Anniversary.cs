using Pickadate.BuildingBlocks.Domain;

namespace Pickadate.Domain.Anniversaries;

/// <summary>
/// Spec §8: once a couple has a completed meeting, pickadate.me remembers
/// the date and fires a mirror notification to both sides on the yearly
/// anniversary. The pair is stored in canonical order (UserAId &lt; UserBId)
/// so there's at most one record per couple regardless of who initiated.
/// </summary>
public class Anniversary : Entity
{
    public Guid Id { get; private set; }
    public Guid UserAId { get; private set; }
    public Guid UserBId { get; private set; }
    public Guid InvitationId { get; private set; }
    public DateTime FirstDateAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Anniversary() { }

    public static Anniversary Create(Guid userOneId, Guid userTwoId, Guid invitationId, DateTime firstDateAtUtc)
    {
        if (userOneId == userTwoId)
            throw new ArgumentException("Anniversary requires two distinct users.");

        // Canonical ordering so queries for a pair always hit the same row.
        var (a, b) = userOneId.CompareTo(userTwoId) < 0 ? (userOneId, userTwoId) : (userTwoId, userOneId);

        return new Anniversary
        {
            Id = Guid.NewGuid(),
            UserAId = a,
            UserBId = b,
            InvitationId = invitationId,
            FirstDateAt = DateTime.SpecifyKind(firstDateAtUtc, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>True when this anniversary falls on today's month and day.</summary>
    public bool IsDueOn(DateTime nowUtc) =>
        FirstDateAt.Month == nowUtc.Month && FirstDateAt.Day == nowUtc.Day;

    /// <summary>How many full years have passed since the first date.</summary>
    public int YearsSince(DateTime nowUtc)
    {
        var years = nowUtc.Year - FirstDateAt.Year;
        if (nowUtc.Month < FirstDateAt.Month || (nowUtc.Month == FirstDateAt.Month && nowUtc.Day < FirstDateAt.Day))
            years--;
        return years;
    }
}

public interface IAnniversaryRepository
{
    Task AddAsync(Anniversary anniversary, CancellationToken ct = default);
    Task<bool> ExistsForPairAsync(Guid userOneId, Guid userTwoId, CancellationToken ct = default);
    Task<IReadOnlyList<Anniversary>> GetDueOnAsync(int month, int day, CancellationToken ct = default);
}
