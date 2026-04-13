using MediatR;
using Pickadate.Application.Contracts;
using Pickadate.Application.Invitations.Commands;
using Pickadate.BuildingBlocks.Application;
using Pickadate.Domain.Invitations;
using Pickadate.Domain.Safety;

namespace Pickadate.Application.Safety.Commands;

public record ConfirmSafetyCheckCommand(string Slug) : ICommand;

public class ConfirmSafetyCheckCommandHandler : IRequestHandler<ConfirmSafetyCheckCommand>
{
    private readonly IInvitationRepository _invitations;
    private readonly ISafetyCheckRepository _safetyChecks;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _uow;

    public ConfirmSafetyCheckCommandHandler(
        IInvitationRepository invitations,
        ISafetyCheckRepository safetyChecks,
        ICurrentUser currentUser,
        IUnitOfWork uow)
    {
        _invitations = invitations;
        _safetyChecks = safetyChecks;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task Handle(ConfirmSafetyCheckCommand request, CancellationToken ct)
    {
        var userId = _currentUser.RequireUserId();

        var invitation = await _invitations.GetBySlugAsync(request.Slug, ct)
            ?? throw new InvitationNotFoundException(request.Slug);

        var check = await _safetyChecks.GetActiveForInvitationAsync(invitation.Id, userId, ct);
        if (check is null) return; // idempotent: already confirmed or never created

        check.Confirm();
        await _uow.CommitAsync(ct);
    }
}
