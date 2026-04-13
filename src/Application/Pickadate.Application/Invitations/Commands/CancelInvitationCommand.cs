using MediatR;
using Pickadate.Application.Invitations.Authorization;
using Pickadate.BuildingBlocks.Application;
using Pickadate.Domain.Invitations;

namespace Pickadate.Application.Invitations.Commands;

public record CancelInvitationCommand(string Slug) : ICommand;

public class CancelInvitationCommandHandler : IRequestHandler<CancelInvitationCommand>
{
    private readonly IInvitationRepository _invitations;
    private readonly IInvitationOwnerAuthorizer _ownerAuthorizer;
    private readonly IUnitOfWork _uow;

    public CancelInvitationCommandHandler(
        IInvitationRepository invitations,
        IInvitationOwnerAuthorizer ownerAuthorizer,
        IUnitOfWork uow)
    {
        _invitations = invitations;
        _ownerAuthorizer = ownerAuthorizer;
        _uow = uow;
    }

    public async Task Handle(CancelInvitationCommand request, CancellationToken ct)
    {
        var invitation = await _invitations.GetBySlugAsync(request.Slug, ct)
            ?? throw new InvitationNotFoundException(request.Slug);

        _ownerAuthorizer.AssertOwns(invitation);

        invitation.Cancel();
        await _uow.CommitAsync(ct);
    }
}
