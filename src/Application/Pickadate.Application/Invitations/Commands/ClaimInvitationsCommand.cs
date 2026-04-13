using FluentValidation;
using MediatR;
using Pickadate.Application.Contracts;
using Pickadate.Application.Invitations.Dtos;
using Pickadate.BuildingBlocks.Application;
using Pickadate.Domain.Invitations;

namespace Pickadate.Application.Invitations.Commands;

public record ClaimInvitationsCommand(IReadOnlyList<string> OwnerTokens) : ICommand<ClaimInvitationsResult>;

public record ClaimInvitationsResult(int Claimed, IReadOnlyList<string> ClaimedSlugs);

public class ClaimInvitationsCommandValidator : AbstractValidator<ClaimInvitationsCommand>
{
    public ClaimInvitationsCommandValidator()
    {
        RuleFor(x => x.OwnerTokens).NotNull();
        RuleForEach(x => x.OwnerTokens).NotEmpty().MaximumLength(64);
    }
}

public class ClaimInvitationsCommandHandler : IRequestHandler<ClaimInvitationsCommand, ClaimInvitationsResult>
{
    private readonly IInvitationRepository _invitations;
    private readonly IOwnerTokenGenerator _ownerTokens;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _uow;

    public ClaimInvitationsCommandHandler(
        IInvitationRepository invitations,
        IOwnerTokenGenerator ownerTokens,
        ICurrentUser currentUser,
        IUnitOfWork uow)
    {
        _invitations = invitations;
        _ownerTokens = ownerTokens;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<ClaimInvitationsResult> Handle(ClaimInvitationsCommand request, CancellationToken ct)
    {
        var userId = _currentUser.RequireUserId();

        // Hash every supplied token once, then look them up in bulk.
        var hashes = request.OwnerTokens
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => _ownerTokens.Hash(t))
            .Distinct()
            .ToList();

        if (hashes.Count == 0)
        {
            return new ClaimInvitationsResult(0, Array.Empty<string>());
        }

        var matches = await _invitations.FindByOwnerTokenHashesAsync(hashes, ct);

        var claimed = new List<string>(matches.Count);
        foreach (var invitation in matches)
        {
            // ClaimByUser is a no-op if the invitation already has an
            // initiator — defensive, since FindByOwnerTokenHashesAsync only
            // returns rows with a non-null hash.
            if (invitation.InitiatorId is null)
            {
                invitation.ClaimByUser(userId);
                claimed.Add(invitation.Slug);
            }
        }

        if (claimed.Count > 0)
        {
            await _uow.CommitAsync(ct);
        }

        return new ClaimInvitationsResult(claimed.Count, claimed);
    }
}
