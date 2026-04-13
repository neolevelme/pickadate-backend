using MediatR;
using Pickadate.Application.Invitations.Authorization;
using Pickadate.BuildingBlocks.Application;
using Pickadate.Domain.Invitations;

namespace Pickadate.Application.Invitations.Commands;

public record AcceptCounterProposalCommand(string Slug) : ICommand;

public class AcceptCounterProposalCommandHandler : IRequestHandler<AcceptCounterProposalCommand>
{
    private readonly IInvitationRepository _invitations;
    private readonly ICounterProposalRepository _counterProposals;
    private readonly IInvitationOwnerAuthorizer _ownerAuthorizer;
    private readonly IUnitOfWork _uow;

    public AcceptCounterProposalCommandHandler(
        IInvitationRepository invitations,
        ICounterProposalRepository counterProposals,
        IInvitationOwnerAuthorizer ownerAuthorizer,
        IUnitOfWork uow)
    {
        _invitations = invitations;
        _counterProposals = counterProposals;
        _ownerAuthorizer = ownerAuthorizer;
        _uow = uow;
    }

    public async Task Handle(AcceptCounterProposalCommand request, CancellationToken ct)
    {
        var invitation = await _invitations.GetBySlugAsync(request.Slug, ct)
            ?? throw new InvitationNotFoundException(request.Slug);

        _ownerAuthorizer.AssertOwns(invitation);

        var latest = await _counterProposals.GetLatestForInvitationAsync(invitation.Id, ct)
            ?? throw new InvitationNotFoundException($"no counter-proposal for '{request.Slug}'");

        invitation.AcceptCounterProposal(latest);
        await _uow.CommitAsync(ct);
    }
}
