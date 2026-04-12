using MediatR;
using Pickadate.Application.Contracts;
using Pickadate.BuildingBlocks.Application;
using Pickadate.Domain.Invitations;

namespace Pickadate.Application.Invitations.Commands;

public record AcceptCounterProposalCommand(string Slug) : ICommand;

public class AcceptCounterProposalCommandHandler : IRequestHandler<AcceptCounterProposalCommand>
{
    private readonly IInvitationRepository _invitations;
    private readonly ICounterProposalRepository _counterProposals;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _uow;

    public AcceptCounterProposalCommandHandler(
        IInvitationRepository invitations,
        ICounterProposalRepository counterProposals,
        ICurrentUser currentUser,
        IUnitOfWork uow)
    {
        _invitations = invitations;
        _counterProposals = counterProposals;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task Handle(AcceptCounterProposalCommand request, CancellationToken ct)
    {
        var userId = _currentUser.RequireUserId();
        var invitation = await _invitations.GetBySlugAsync(request.Slug, ct)
            ?? throw new InvitationNotFoundException(request.Slug);

        if (invitation.InitiatorId != userId)
            throw new UnauthorizedAccessException("Only the initiator can accept a counter-proposal here.");

        var latest = await _counterProposals.GetLatestForInvitationAsync(invitation.Id, ct)
            ?? throw new InvitationNotFoundException($"no counter-proposal for '{request.Slug}'");

        invitation.AcceptCounterProposal(latest);
        await _uow.CommitAsync(ct);
    }
}
