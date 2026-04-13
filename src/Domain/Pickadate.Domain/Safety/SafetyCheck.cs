using Pickadate.BuildingBlocks.Domain;

namespace Pickadate.Domain.Safety;

/// <summary>
/// Spec §7: the accepted-side user creates a shareable friend link with
/// the meeting details and an auto check-in time (2h after the meeting
/// starts by default). The user can press "all good" earlier to clear
/// the alert; if they don't, a background job notifies the friend once
/// the check-in time has passed.
///
/// This aggregate owns the state for one such check. Alerting the friend
/// is a Phase 5 concern (push) — for now we record the state and the
/// hosted service just logs when an alert would fire.
/// </summary>
public class SafetyCheck : Entity
{
    // Default grace period after the meeting start time. Spec §7 says 2h.
    public static readonly TimeSpan DefaultGracePeriod = TimeSpan.FromHours(2);

    public Guid Id { get; private set; }
    public Guid InvitationId { get; private set; }
    public Guid UserId { get; private set; }
    public string FriendToken { get; private set; } = null!;
    public DateTime ScheduledCheckInAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? AlertedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private SafetyCheck() { }

    public static SafetyCheck Create(
        Guid invitationId,
        Guid userId,
        string friendToken,
        DateTime meetingAtUtc,
        TimeSpan? gracePeriod = null)
    {
        if (string.IsNullOrWhiteSpace(friendToken))
            throw new ArgumentException("Friend token is required.", nameof(friendToken));

        var grace = gracePeriod ?? DefaultGracePeriod;
        var scheduled = DateTime.SpecifyKind(meetingAtUtc, DateTimeKind.Utc) + grace;

        return new SafetyCheck
        {
            Id = Guid.NewGuid(),
            InvitationId = invitationId,
            UserId = userId,
            FriendToken = friendToken,
            ScheduledCheckInAt = scheduled,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>User tells us they're fine ahead of time — clears the scheduled alert.</summary>
    public void Confirm()
    {
        if (ConfirmedAt is not null) return;
        ConfirmedAt = DateTime.UtcNow;
    }

    /// <summary>Background service marks an unconfirmed check as alerted once its check-in time has passed.</summary>
    public void MarkAlerted()
    {
        AlertedAt = DateTime.UtcNow;
    }

    public bool NeedsAlerting(DateTime nowUtc) =>
        ConfirmedAt is null && AlertedAt is null && nowUtc >= ScheduledCheckInAt;
}

public interface ISafetyCheckRepository
{
    Task AddAsync(SafetyCheck check, CancellationToken ct = default);
    Task<SafetyCheck?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<SafetyCheck?> GetByFriendTokenAsync(string friendToken, CancellationToken ct = default);
    Task<SafetyCheck?> GetActiveForInvitationAsync(Guid invitationId, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<SafetyCheck>> GetDueForAlertAsync(DateTime nowUtc, CancellationToken ct = default);
}
