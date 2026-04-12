using Pickadate.BuildingBlocks.Domain;

namespace Pickadate.Domain.Invitations;

public enum CounterProposalKind
{
    TimeOnly = 0,
    PlaceOnly = 1,
    Both = 2
}

public class CounterProposal : Entity
{
    public Guid Id { get; private set; }
    public Guid InvitationId { get; private set; }
    public int Round { get; private set; }
    public Guid ProposerId { get; private set; }
    public CounterProposalKind Kind { get; private set; }
    public DateTime? NewMeetingAt { get; private set; }
    public Place? NewPlace { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private CounterProposal() { }

    public static CounterProposal Create(
        Guid invitationId,
        int round,
        Guid proposerId,
        DateTime? newMeetingAtUtc,
        Place? newPlace)
    {
        var hasTime = newMeetingAtUtc.HasValue;
        var hasPlace = newPlace is not null;

        if (!hasTime && !hasPlace)
            throw new ArgumentException("A counter-proposal must change time, place, or both.");

        if (hasTime && newMeetingAtUtc!.Value <= DateTime.UtcNow)
            throw new ArgumentException("New meeting time must be in the future.");

        var kind = (hasTime, hasPlace) switch
        {
            (true, true) => CounterProposalKind.Both,
            (true, false) => CounterProposalKind.TimeOnly,
            (false, true) => CounterProposalKind.PlaceOnly,
            _ => throw new InvalidOperationException()
        };

        return new CounterProposal
        {
            Id = Guid.NewGuid(),
            InvitationId = invitationId,
            Round = round,
            ProposerId = proposerId,
            Kind = kind,
            NewMeetingAt = hasTime ? DateTime.SpecifyKind(newMeetingAtUtc!.Value, DateTimeKind.Utc) : null,
            NewPlace = newPlace,
            CreatedAt = DateTime.UtcNow
        };
    }
}
