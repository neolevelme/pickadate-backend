using MediatR;
using Pickadate.Application.Contracts;
using Pickadate.Application.Invitations.Dtos;
using Pickadate.BuildingBlocks.Application;
using Pickadate.Domain.Invitations;

namespace Pickadate.Application.Invitations.Queries;

public record GetMyInvitationsQuery : IQuery<IReadOnlyList<InvitationDetailDto>>;

public class GetMyInvitationsQueryHandler : IRequestHandler<GetMyInvitationsQuery, IReadOnlyList<InvitationDetailDto>>
{
    private readonly IInvitationRepository _invitations;
    private readonly ICounterProposalRepository _counterProposals;
    private readonly IWeatherService _weather;
    private readonly ICurrentUser _currentUser;

    public GetMyInvitationsQueryHandler(
        IInvitationRepository invitations,
        ICounterProposalRepository counterProposals,
        IWeatherService weather,
        ICurrentUser currentUser)
    {
        _invitations = invitations;
        _counterProposals = counterProposals;
        _weather = weather;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<InvitationDetailDto>> Handle(GetMyInvitationsQuery request, CancellationToken ct)
    {
        var userId = _currentUser.RequireUserId();
        var list = await _invitations.ListForInitiatorAsync(userId, ct);

        var results = new List<InvitationDetailDto>(list.Count);
        foreach (var i in list)
        {
            CounterProposalDto? latest = null;
            if (i.CounterRound > 0)
            {
                var c = await _counterProposals.GetLatestForInvitationAsync(i.Id, ct);
                if (c is not null)
                {
                    latest = new CounterProposalDto(
                        c.Round,
                        c.Kind.ToString(),
                        c.NewMeetingAt,
                        c.NewPlace is null
                            ? null
                            : new PlaceDto(c.NewPlace.GooglePlaceId, c.NewPlace.Name, c.NewPlace.FormattedAddress, c.NewPlace.Lat, c.NewPlace.Lng),
                        c.CreatedAt);
                }
            }

            var forecast = await _weather.GetForecastAsync(i.Place.Lat, i.Place.Lng, i.MeetingAt, ct);
            var weatherDto = forecast is null
                ? null
                : new WeatherDto(forecast.TemperatureMaxC, forecast.TemperatureMinC, forecast.PrecipitationMm, forecast.WeatherCode, forecast.Description);

            results.Add(new InvitationDetailDto(
                Slug: i.Slug,
                Vibe: i.Vibe.ToString(),
                CustomVibe: i.CustomVibe,
                Place: new PlaceDto(i.Place.GooglePlaceId, i.Place.Name, i.Place.FormattedAddress, i.Place.Lat, i.Place.Lng),
                MeetingAt: i.MeetingAt,
                Message: i.Message,
                MediaUrl: i.MediaUrl,
                Status: i.Status.ToString(),
                CounterRound: i.CounterRound,
                MaxCounterRounds: Invitation.MaxCounterRounds,
                CreatedAt: i.CreatedAt,
                ExpiresAt: i.ExpiresAt,
                // The initiator *is* the caller, so we don't need to look the user up.
                // "You" keeps the DTO shape consistent with the public view.
                InitiatorName: "You",
                LatestCounter: latest,
                Weather: weatherDto,
                ViewerIsOwner: true,
                HasAccount: true));
        }

        return results;
    }
}
