using MediatR;
using Pickadate.Application.Contracts;
using Pickadate.Application.Invitations.Authorization;
using Pickadate.BuildingBlocks.Application;
using Pickadate.Domain.Invitations;

namespace Pickadate.Application.Invitations.Commands;

public record AcceptInvitationCommand(string Slug) : ICommand;

public class AcceptInvitationCommandHandler : IRequestHandler<AcceptInvitationCommand>
{
    private readonly IInvitationRepository _invitations;
    private readonly ICurrentUser _currentUser;
    private readonly IInvitationOwnerAuthorizer _ownerAuthorizer;
    private readonly INotificationService _notifications;
    private readonly IUnitOfWork _uow;

    public AcceptInvitationCommandHandler(
        IInvitationRepository invitations,
        ICurrentUser currentUser,
        IInvitationOwnerAuthorizer ownerAuthorizer,
        INotificationService notifications,
        IUnitOfWork uow)
    {
        _invitations = invitations;
        _currentUser = currentUser;
        _ownerAuthorizer = ownerAuthorizer;
        _notifications = notifications;
        _uow = uow;
    }

    public async Task Handle(AcceptInvitationCommand request, CancellationToken ct)
    {
        // The recipient side still requires an account — anniversary mode
        // and notifications need a stable identity.
        var userId = _currentUser.RequireUserId();

        var invitation = await _invitations.GetBySlugAsync(request.Slug, ct)
            ?? throw new InvitationNotFoundException(request.Slug);

        // You can't accept your own invitation. The frontend hides the button
        // when ViewerIsOwner is true, but enforce it here too so a forged
        // request can't sneak through.
        if (_ownerAuthorizer.IsOwner(invitation) || invitation.InitiatorId == userId)
        {
            throw new CannotAcceptOwnInvitationException();
        }

        invitation.Accept(userId);
        await _uow.CommitAsync(ct);

        // Fire-and-persist after commit so a flaky push transport never blocks
        // the accept itself. Only notify when the initiator actually has an
        // account to receive it; anonymous creators see the new state next
        // time they open /dashboard.
        if (invitation.InitiatorId is Guid initiatorId)
        {
            await _notifications.NotifyUserAsync(
                initiatorId,
                new NotificationPayload(
                    Title: "Your invitation was accepted ✨",
                    Body: $"{invitation.Place.Name} — see you there.",
                    Url: $"/dashboard",
                    Tag: $"invitation-accepted-{invitation.Slug}"),
                ct);
        }
    }
}

public class InvitationNotFoundException : Exception
{
    public InvitationNotFoundException(string slug) : base($"Invitation '{slug}' not found.") { }
}

public class CannotAcceptOwnInvitationException : Exception
{
    public CannotAcceptOwnInvitationException()
        : base("You can't accept your own invitation.") { }
}
