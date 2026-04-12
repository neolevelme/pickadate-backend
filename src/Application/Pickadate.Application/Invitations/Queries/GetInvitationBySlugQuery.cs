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
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;

    public GetInvitationBySlugQueryHandler(
        IInvitationRepository invitations,
        IUserRepository users,
        IUnitOfWork uow)
    {
        _invitations = invitations;
        _users = users;
        _uow = uow;
    }

    public async Task<InvitationDetailDto?> Handle(GetInvitationBySlugQuery request, CancellationToken ct)
    {
        var invitation = await _invitations.GetBySlugAsync(request.Slug, ct);
        if (invitation is null) return null;

        // Record the first view so the initiator can see "opened" state later.
        var wasPending = invitation.Status == InvitationStatus.Pending;
        invitation.RecordView(DateTime.UtcNow);
        if (wasPending) await _uow.CommitAsync(ct);

        var initiator = await _users.GetByIdAsync(invitation.InitiatorId, ct);

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
            CreatedAt: invitation.CreatedAt,
            ExpiresAt: invitation.ExpiresAt,
            InitiatorName: initiator?.Name ?? "Someone");
    }
}
