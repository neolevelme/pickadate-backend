using MediatR;
using Pickadate.Application.Contracts;
using Pickadate.Application.Invitations.Authorization;
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
    private readonly IWeatherService _weather;
    private readonly INotificationService _notifications;
    private readonly IInvitationOwnerAuthorizer _ownerAuthorizer;
    private readonly IUnitOfWork _uow;

    public GetInvitationBySlugQueryHandler(
        IInvitationRepository invitations,
        ICounterProposalRepository counterProposals,
        IUserRepository users,
        IWeatherService weather,
        INotificationService notifications,
        IInvitationOwnerAuthorizer ownerAuthorizer,
        IUnitOfWork uow)
    {
        _invitations = invitations;
        _counterProposals = counterProposals;
        _users = users;
        _weather = weather;
        _notifications = notifications;
        _ownerAuthorizer = ownerAuthorizer;
        _uow = uow;
    }

    public async Task<InvitationDetailDto?> Handle(GetInvitationBySlugQuery request, CancellationToken ct)
    {
        var invitation = await _invitations.GetBySlugAsync(request.Slug, ct);
        if (invitation is null) return null;

        var viewerIsOwner = _ownerAuthorizer.IsOwner(invitation);

        // Don't bump Pending → Viewed when the *creator themselves* is opening
        // the link (e.g. checking it from a second browser). We only want to
        // count the recipient's first open.
        var wasPending = invitation.Status == InvitationStatus.Pending && !viewerIsOwner;
        if (!viewerIsOwner)
        {
            invitation.RecordView(DateTime.UtcNow);
        }
        if (wasPending)
        {
            await _uow.CommitAsync(ct);

            // Spec §9: notify the initiator the moment their link is first
            // opened — but only when they actually have an account to receive
            // the push. Anonymous initiators get the same signal next time
            // they open the dashboard.
            if (invitation.InitiatorId is Guid initiatorId)
            {
                await _notifications.NotifyUserAsync(
                    initiatorId,
                    new NotificationPayload(
                        Title: "Your invitation was opened",
                        Body: "They've seen it — fingers crossed.",
                        Url: "/dashboard",
                        Tag: $"invitation-viewed-{invitation.Slug}"),
                    ct);
            }
        }

        var initiator = invitation.InitiatorId is Guid id
            ? await _users.GetByIdAsync(id, ct)
            : null;

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

        var weather = await _weather.GetForecastAsync(invitation.Place.Lat, invitation.Place.Lng, invitation.MeetingAt, ct);
        var weatherDto = weather is null
            ? null
            : new WeatherDto(weather.TemperatureMaxC, weather.TemperatureMinC, weather.PrecipitationMm, weather.WeatherCode, weather.Description);

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
            LatestCounter: latestCounterDto,
            Weather: weatherDto,
            ViewerIsOwner: viewerIsOwner,
            HasAccount: invitation.InitiatorId is not null);
    }
}
