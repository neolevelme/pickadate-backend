using MediatR;
using Pickadate.Application.Contracts;
using Pickadate.BuildingBlocks.Application;
using Pickadate.Domain.Invitations;

namespace Pickadate.Application.Invitations.Commands;

public record MarkCompletedCommand(string Slug) : ICommand;

public class MarkCompletedCommandHandler : IRequestHandler<MarkCompletedCommand>
{
    private readonly IInvitationRepository _invitations;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _uow;

    public MarkCompletedCommandHandler(
        IInvitationRepository invitations,
        ICurrentUser currentUser,
        IUnitOfWork uow)
    {
        _invitations = invitations;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task Handle(MarkCompletedCommand request, CancellationToken ct)
    {
        var userId = _currentUser.RequireUserId();
        var invitation = await _invitations.GetBySlugAsync(request.Slug, ct)
            ?? throw new InvitationNotFoundException(request.Slug);

        // Spec §10: "Označi sastanak kao završen" is available to the initiator
        // and (once recipient tracking lands) the recipient. For now we allow
        // the initiator only, since that's who has the dashboard in Phase 6.
        if (invitation.InitiatorId != userId)
            throw new UnauthorizedAccessException("Only the initiator can mark this invitation completed.");

        invitation.MarkCompleted();
        await _uow.CommitAsync(ct);
    }
}
