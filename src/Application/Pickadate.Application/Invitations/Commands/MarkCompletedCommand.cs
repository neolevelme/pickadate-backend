using MediatR;
using Pickadate.Application.Invitations.Authorization;
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
    private readonly IInvitationOwnerAuthorizer _ownerAuthorizer;
    private readonly IUnitOfWork _uow;

    public MarkCompletedCommandHandler(
        IInvitationRepository invitations,
        IAnniversaryRepository anniversaries,
        IUserRepository users,
        IInvitationOwnerAuthorizer ownerAuthorizer,
        IUnitOfWork uow)
    {
        _invitations = invitations;
        _anniversaries = anniversaries;
        _users = users;
        _ownerAuthorizer = ownerAuthorizer;
        _uow = uow;
    }

    public async Task Handle(MarkCompletedCommand request, CancellationToken ct)
    {
        var invitation = await _invitations.GetBySlugAsync(request.Slug, ct)
            ?? throw new InvitationNotFoundException(request.Slug);

        _ownerAuthorizer.AssertOwns(invitation);

        invitation.MarkCompleted();

        // Spec §8: a successful meeting seeds an Anniversary record for the
        // couple, but only when both sides actually have accounts. Anonymous
        // invitations never seed an anniversary because there's no second
        // user to notify.
        if (invitation.InitiatorId is Guid initiatorId && invitation.RecipientId is Guid recipientId)
        {
            await TrySeedAnniversary(invitation.Id, initiatorId, recipientId, invitation.MeetingAt, ct);
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
