using MediatR;
using Pickadate.Application.Contracts;
using Pickadate.BuildingBlocks.Application;
using Pickadate.Domain.Invitations;

namespace Pickadate.Application.Invitations.Commands;

public record CancelInvitationCommand(string Slug) : ICommand;

public class CancelInvitationCommandHandler : IRequestHandler<CancelInvitationCommand>
{
    private readonly IInvitationRepository _invitations;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _uow;

    public CancelInvitationCommandHandler(
        IInvitationRepository invitations,
        ICurrentUser currentUser,
        IUnitOfWork uow)
    {
        _invitations = invitations;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task Handle(CancelInvitationCommand request, CancellationToken ct)
    {
        var userId = _currentUser.RequireUserId();
        var invitation = await _invitations.GetBySlugAsync(request.Slug, ct)
            ?? throw new InvitationNotFoundException(request.Slug);

        if (invitation.InitiatorId != userId)
            throw new UnauthorizedAccessException("Only the initiator can cancel this invitation.");

        invitation.Cancel();
        await _uow.CommitAsync(ct);
    }
}
