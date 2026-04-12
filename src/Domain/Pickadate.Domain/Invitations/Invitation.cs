using Pickadate.BuildingBlocks.Domain;

namespace Pickadate.Domain.Invitations;

public class Invitation : Entity
{
    // Spec §3 korak 5: unopened link expires after 72h.
    private static readonly TimeSpan UnopenedTtl = TimeSpan.FromHours(72);

    public Guid Id { get; private set; }
    public Guid InitiatorId { get; private set; }
    public string Slug { get; private set; } = null!;

    public InvitationVibe Vibe { get; private set; }
    public string? CustomVibe { get; private set; }

    // Place is persisted as owned columns via EF — see DbContext.
    public Place Place { get; private set; } = null!;

    public DateTime MeetingAt { get; private set; }
    public string? Message { get; private set; }
    public string? MediaUrl { get; private set; }

    public InvitationStatus Status { get; private set; }
    public int CounterRound { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? FirstViewedAt { get; private set; }

    private Invitation() { }

    /// <summary>
    /// Creates an already-published invitation ready to be shared.
    /// MVP for Faza 2 — the wizard posts once and the recipient gets a live link.
    /// A draft-only state will be introduced later if we add autosave.
    /// </summary>
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
