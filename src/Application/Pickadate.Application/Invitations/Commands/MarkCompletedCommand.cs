using MediatR;
using Pickadate.Application.Contracts;
using Pickadate.BuildingBlocks.Application;
using Pickadate.Domain.Anniversaries;
using Pickadate.Domain.Invitations;
using Pickadate.Domain.Users;

namespace Pickadate.Application.Invitations.Commands;

public record MarkCompletedCommand(string Slug) : ICommand;

public class MarkCompletedCommandHandler : IRequestHandler<MarkCompletedCommand>
{
    private readonly IInvitationRepository _invitations;
    private readonly IAnniversaryRepository _anniversaries;
    private readonly IUserRepository _users;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _uow;

    public MarkCompletedCommandHandler(
        IInvitationRepository invitations,
        IAnniversaryRepository anniversaries,
        IUserRepository users,
        ICurrentUser currentUser,
        IUnitOfWork uow)
    {
        _invitations = invitations;
        _anniversaries = anniversaries;
        _users = users;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task Handle(MarkCompletedCommand request, CancellationToken ct)
    {
        var userId = _currentUser.RequireUserId();
        var invitation = await _invitations.GetBySlugAsync(request.Slug, ct)
            ?? throw new InvitationNotFoundException(request.Slug);

        // Spec §10: the initiator can mark completed from the dashboard. Once
        // the recipient also gets a dashboard view they can do the same.
        if (invitation.InitiatorId != userId)
            throw new UnauthorizedAccessException("Only the initiator can mark this invitation completed.");

        invitation.MarkCompleted();

        // Spec §8: a successful meeting seeds an Anniversary record for the
        // couple if both sides have anniversary reminders enabled and no
        // record exists yet for this pair. RecipientId is only populated when
        // someone authenticated to accept, so anonymous-view invitations don't
        // seed anniversaries.
        if (invitation.RecipientId is Guid recipientId)
        {
            await TrySeedAnniversary(invitation.Id, invitation.InitiatorId, recipientId, invitation.MeetingAt, ct);
        }

        await _uow.CommitAsync(ct);
    }

    private async Task TrySeedAnniversary(Guid invitationId, Guid initiatorId, Guid recipientId, DateTime firstDateAt, CancellationToken ct)
    {
        var exists = await _anniversaries.ExistsForPairAsync(initiatorId, recipientId, ct);
        if (exists) return;

        var initiator = await _users.GetByIdAsync(initiatorId, ct);
        var recipient = await _users.GetByIdAsync(recipientId, ct);

        if (initiator is null || recipient is null) return;
        if (!initiator.AnniversaryEnabled || !recipient.AnniversaryEnabled) return;

        var anniversary = Anniversary.Create(initiatorId, recipientId, invitationId, firstDateAt);
        await _anniversaries.AddAsync(anniversary, ct);
    }
}
