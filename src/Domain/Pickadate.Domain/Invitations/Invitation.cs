using Pickadate.BuildingBlocks.Domain;

namespace Pickadate.Domain.Invitations;

// Skeleton — full state machine and validation rules arrive in Faza 2.
public class Invitation : Entity
{
    public Guid Id { get; private set; }
    public Guid InitiatorId { get; private set; }
    public string Slug { get; private set; } = null!;
    public InvitationVibe Vibe { get; private set; }
    public string? CustomVibe { get; private set; }
    public string PlaceName { get; private set; } = null!;
    public string PlaceGoogleId { get; private set; } = null!;
    public double PlaceLat { get; private set; }
    public double PlaceLng { get; private set; }
    public string PlaceFormattedAddress { get; private set; } = null!;
    public DateTime MeetingAt { get; private set; }
    public string? Message { get; private set; }
    public string? MediaUrl { get; private set; }
    public InvitationStatus Status { get; private set; }
    public int CounterRound { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    private Invitation() { }
}
