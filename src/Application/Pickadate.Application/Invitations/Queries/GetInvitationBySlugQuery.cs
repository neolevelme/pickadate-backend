using MediatR;
using Pickadate.Application.Invitations.Dtos;
using Pickadate.BuildingBlocks.Application;
using Pickadate.Domain.Invitations;
using Pickadate.Domain.Users;

namespace Pickadate.Application.Invitations.Queries;

public record GetInvitationBySlugQuery(string Slug) : IQuery<InvitationDetailDto?>;

public class GetInvitationBySlugQueryHandler : IRequestHandler<GetInvitationBySlugQuery, InvitationDetailDto?>
{
    private readonly IInvitationRepository _invitations;
    private readonly ICounterProposalRepository _counterProposals;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;

    public GetInvitationBySlugQueryHandler(
        IInvitationRepository invitations,
        ICounterProposalRepository counterProposals,
        IUserRepository users,
        IUnitOfWork uow)
    {
        _invitations = invitations;
        _counterProposals = counterProposals;
        _users = users;
        _uow = uow;
    }

    public async Task<InvitationDetailDto?> Handle(GetInvitationBySlugQuery request, CancellationToken ct)
    {
        var invitation = await _invitations.GetBySlugAsync(request.Slug, ct);
        if (invitation is null) return null;

        var wasPending = invitation.Status == InvitationStatus.Pending;
        invitation.RecordView(DateTime.UtcNow);
        if (wasPending) await _uow.CommitAsync(ct);

        var initiator = await _users.GetByIdAsync(invitation.InitiatorId, ct);

        CounterProposalDto? latestCounterDto = null;
        if (invitation.CounterRound > 0)
        {
            var latest = await _counterProposals.GetLatestForInvitationAsync(invitation.Id, ct);
            if (latest is not null)
            {
                latestCounterDto = new CounterProposalDto(
                    Round: latest.Round,
                    Kind: latest.Kind.ToString(),
                    NewMeetingAt: latest.NewMeetingAt,
                    NewPlace: latest.NewPlace is null
                        ? null
                        : new PlaceDto(
                            latest.NewPlace.GooglePlaceId,
                            latest.NewPlace.Name,
                            latest.NewPlace.FormattedAddress,
                            latest.NewPlace.Lat,
                            latest.NewPlace.Lng),
                    CreatedAt: latest.CreatedAt);
            }
        }

        return new InvitationDetailDto(
            Slug: invitation.Slug,
            Vibe: invitation.Vibe.ToString(),
            CustomVibe: invitation.CustomVibe,
            Place: new PlaceDto(
                invitation.Place.GooglePlaceId,
                invitation.Place.Name,
                invitation.Place.FormattedAddress,
                invitation.Place.Lat,
                invitation.Place.Lng),
            MeetingAt: invitation.MeetingAt,
            Message: invitation.Message,
            MediaUrl: invitation.MediaUrl,
            Status: invitation.Status.ToString(),
            CounterRound: invitation.CounterRound,
            MaxCounterRounds: Invitation.MaxCounterRounds,
            CreatedAt: invitation.CreatedAt,
            ExpiresAt: invitation.ExpiresAt,
            InitiatorName: initiator?.Name ?? "Someone",
            LatestCounter: latestCounterDto);
    }
}
