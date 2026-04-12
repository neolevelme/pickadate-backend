using Pickadate.BuildingBlocks.Domain;

namespace Pickadate.Domain.Invitations;

public class Invitation : Entity
{
    // Spec §3 korak 5: unopened link expires after 72h.
    private static readonly TimeSpan UnopenedTtl = TimeSpan.FromHours(72);

    // Spec §4 Opcija 2: at most 3 counter-proposal rounds total.
    public const int MaxCounterRounds = 3;

    public Guid Id { get; private set; }
    public Guid InitiatorId { get; private set; }
    public string Slug { get; private set; } = null!;

    public InvitationVibe Vibe { get; private set; }
    public string? CustomVibe { get; private set; }

    public Place Place { get; private set; } = null!;

    public DateTime MeetingAt { get; private set; }
    public string? Message { get; private set; }
    public string? MediaUrl { get; private set; }

    public InvitationStatus Status { get; private set; }
    public int CounterRound { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? FirstViewedAt { get; private set; }
    public DateTime? RespondedAt { get; private set; }
    public string? DeclineReason { get; private set; }

    private Invitation() { }

    public static Invitation CreateAndPublish(
        Guid initiatorId,
        string slug,
        InvitationVibe vibe,
        string? customVibe,
        Place place,
        DateTime meetingAtUtc,
        string? message,
        string? mediaUrl)
    {
        CheckRule(new MeetingMustBeInTheFuture(meetingAtUtc));
        CheckRule(new MessageLengthWithinLimit(message));
        CheckRule(new CustomVibeRequiredWhenVibeIsCustom(vibe, customVibe));

        var now = DateTime.UtcNow;
        return new Invitation
        {
            Id = Guid.NewGuid(),
            InitiatorId = initiatorId,
            Slug = slug,
            Vibe = vibe,
            CustomVibe = vibe == InvitationVibe.Custom ? customVibe?.Trim() : null,
            Place = place,
            MeetingAt = DateTime.SpecifyKind(meetingAtUtc, DateTimeKind.Utc),
            Message = string.IsNullOrWhiteSpace(message) ? null : message.Trim(),
            MediaUrl = string.IsNullOrWhiteSpace(mediaUrl) ? null : mediaUrl.Trim(),
            Status = InvitationStatus.Pending,
            CounterRound = 0,
            CreatedAt = now,
            ExpiresAt = now + UnopenedTtl
        };
    }

    public void RecordView(DateTime nowUtc)
    {
        if (Status == InvitationStatus.Pending)
        {
            Status = InvitationStatus.Viewed;
        }
        FirstViewedAt ??= nowUtc;
    }

    /// <summary>Spec §4 Opcija 1: recipient accepts the invitation (auth required upstream).</summary>
    public void Accept()
    {
        CheckRule(new CanBeRespondedTo(Status));
        Status = InvitationStatus.Accepted;
        RespondedAt = DateTime.UtcNow;
    }

    /// <summary>Spec §4 Opcija 3: recipient declines (anonymous allowed). Comment is optional (≤80 chars).</summary>
    public void Decline(string? reason)
    {
        CheckRule(new CanBeRespondedTo(Status));
        CheckRule(new DeclineReasonWithinLimit(reason));
        Status = InvitationStatus.Declined;
        DeclineReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        RespondedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Spec §4 Opcija 2: recipient sends a counter-proposal for time, place, or both.
    /// Increments the round and flips status to Countered. Rejects when 3 rounds are already spent.
    /// </summary>
    public CounterProposal CounterPropose(Guid proposerId, DateTime? newMeetingAtUtc, Place? newPlace)
    {
        CheckRule(new CanBeRespondedTo(Status));
        CheckRule(new CounterRoundsNotExhausted(CounterRound));

        var nextRound = CounterRound + 1;
        var counter = CounterProposal.Create(Id, nextRound, proposerId, newMeetingAtUtc, newPlace);

        CounterRound = nextRound;
        Status = InvitationStatus.Countered;
        RespondedAt = DateTime.UtcNow;

        // Auto-close when the max round count is reached — spec §4 Opcija 2 "nakon 3 runde
        // automatski se zatvara ako nema dogovora". This is a conservative reading: the
        // third counter is the last allowed action and the invitation expires right after.
        if (CounterRound >= MaxCounterRounds)
        {
            Status = InvitationStatus.Expired;
        }

        return counter;
    }
}

internal sealed class MeetingMustBeInTheFuture : IBusinessRule
{
    private readonly DateTime _meetingAt;
    public MeetingMustBeInTheFuture(DateTime meetingAt) => _meetingAt = meetingAt;
    public bool IsBroken() => _meetingAt <= DateTime.UtcNow;
    public string Message => "Meeting time must be in the future.";
}

internal sealed class MessageLengthWithinLimit : IBusinessRule
{
    private const int Max = 140;
    private readonly string? _message;
    public MessageLengthWithinLimit(string? message) => _message = message;
    public bool IsBroken() => (_message?.Length ?? 0) > Max;
    public string Message => $"Message must be at most {Max} characters.";
}

internal sealed class CustomVibeRequiredWhenVibeIsCustom : IBusinessRule
{
    private readonly InvitationVibe _vibe;
    private readonly string? _customVibe;
    public CustomVibeRequiredWhenVibeIsCustom(InvitationVibe vibe, string? customVibe)
    {
        _vibe = vibe;
        _customVibe = customVibe;
    }
    public bool IsBroken() => _vibe == InvitationVibe.Custom && string.IsNullOrWhiteSpace(_customVibe);
    public string Message => "Custom vibe requires a label.";
}

internal sealed class CanBeRespondedTo : IBusinessRule
{
    private readonly InvitationStatus _status;
    public CanBeRespondedTo(InvitationStatus status) => _status = status;
    public bool IsBroken() => _status is not (InvitationStatus.Pending or InvitationStatus.Viewed or InvitationStatus.Countered);
    public string Message => "This invitation is no longer open for a response.";
}

internal sealed class DeclineReasonWithinLimit : IBusinessRule
{
    private const int Max = 80;
    private readonly string? _reason;
    public DeclineReasonWithinLimit(string? reason) => _reason = reason;
    public bool IsBroken() => (_reason?.Length ?? 0) > Max;
    public string Message => $"Decline reason must be at most {Max} characters.";
}

internal sealed class CounterRoundsNotExhausted : IBusinessRule
{
    private readonly int _round;
    public CounterRoundsNotExhausted(int round) => _round = round;
    public bool IsBroken() => _round >= Invitation.MaxCounterRounds;
    public string Message => $"Counter-proposal rounds exhausted (max {Invitation.MaxCounterRounds}).";
}
