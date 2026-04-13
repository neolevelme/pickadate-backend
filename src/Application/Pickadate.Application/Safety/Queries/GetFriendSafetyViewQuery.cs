using MediatR;
using Pickadate.Application.Safety.Dtos;
using Pickadate.BuildingBlocks.Application;
using Pickadate.Domain.Invitations;
using Pickadate.Domain.Safety;
using Pickadate.Domain.Users;

namespace Pickadate.Application.Safety.Queries;

/// <summary>
/// Public endpoint — anyone with the friend token can see the meeting
/// details and the current check-in status. No user data beyond the
/// initiator's first name is returned.
/// </summary>
public record GetFriendSafetyViewQuery(string FriendToken) : IQuery<FriendSafetyViewDto?>;

public class GetFriendSafetyViewQueryHandler : IRequestHandler<GetFriendSafetyViewQuery, FriendSafetyViewDto?>
{
    private readonly ISafetyCheckRepository _safetyChecks;
    private readonly IInvitationRepository _invitations;
    private readonly IUserRepository _users;

    public GetFriendSafetyViewQueryHandler(
        ISafetyCheckRepository safetyChecks,
        IInvitationRepository invitations,
        IUserRepository users)
    {
        _safetyChecks = safetyChecks;
        _invitations = invitations;
        _users = users;
    }

    public async Task<FriendSafetyViewDto?> Handle(GetFriendSafetyViewQuery request, CancellationToken ct)
    {
        var check = await _safetyChecks.GetByFriendTokenAsync(request.FriendToken, ct);
        if (check is null) return null;

        var invitation = await _invitations.GetByIdAsync(check.InvitationId, ct);
        if (invitation is null) return null;

        var user = await _users.GetByIdAsync(check.UserId, ct);

        var status = check.ConfirmedAt is not null
            ? "Confirmed"
            : (DateTime.UtcNow >= check.ScheduledCheckInAt ? "Overdue" : "Scheduled");

        return new FriendSafetyViewDto(
            InitiatorName: user?.Name ?? "Your friend",
            PlaceName: invitation.Place.Name,
            PlaceFormattedAddress: invitation.Place.FormattedAddress,
            PlaceLat: invitation.Place.Lat,
            PlaceLng: invitation.Place.Lng,
            MeetingAt: invitation.MeetingAt,
            ScheduledCheckInAt: check.ScheduledCheckInAt,
            ConfirmedAt: check.ConfirmedAt,
            Status: status);
    }
}
