using MediatR;
using Pickadate.Application.Contracts;
using Pickadate.BuildingBlocks.Application;
using Pickadate.Domain.Invitations;

namespace Pickadate.Application.Invitations.Commands;

public record AcceptInvitationCommand(string Slug) : ICommand;

public class AcceptInvitationCommandHandler : IRequestHandler<AcceptInvitationCommand>
{
    private readonly IInvitationRepository _invitations;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _uow;

    public AcceptInvitationCommandHandler(
        IInvitationRepository invitations,
        ICurrentUser currentUser,
        IUnitOfWork uow)
    {
        _invitations = invitations;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task Handle(AcceptInvitationCommand request, CancellationToken ct)
    {
        // Auth is enforced at the controller layer (Authorize attribute), but
        // we still require the claim here so the command is safe to dispatch
        // from anywhere.
        var userId = _currentUser.RequireUserId();

        var invitation = await _invitations.GetBySlugAsync(request.Slug, ct)
            ?? throw new InvitationNotFoundException(request.Slug);

        invitation.Accept(userId);
        await _uow.CommitAsync(ct);
    }
}

public class InvitationNotFoundException : Exception
{
    public InvitationNotFoundException(string slug) : base($"Invitation '{slug}' not found.") { }
}
