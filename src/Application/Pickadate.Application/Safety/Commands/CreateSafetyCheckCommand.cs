using MediatR;
using Pickadate.Application.Contracts;
using Pickadate.Application.Invitations.Commands;
using Pickadate.Application.Safety.Dtos;
using Pickadate.BuildingBlocks.Application;
using Pickadate.Domain.Invitations;
using Pickadate.Domain.Safety;

namespace Pickadate.Application.Safety.Commands;

public record CreateSafetyCheckCommand(string Slug) : ICommand<SafetyCheckDto>;

public class CreateSafetyCheckCommandHandler : IRequestHandler<CreateSafetyCheckCommand, SafetyCheckDto>
{
    private readonly IInvitationRepository _invitations;
    private readonly ISafetyCheckRepository _safetyChecks;
    private readonly ISafetyTokenGenerator _tokens;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _uow;

    public CreateSafetyCheckCommandHandler(
        IInvitationRepository invitations,
        ISafetyCheckRepository safetyChecks,
        ISafetyTokenGenerator tokens,
        ICurrentUser currentUser,
        IUnitOfWork uow)
    {
        _invitations = invitations;
        _safetyChecks = safetyChecks;
        _tokens = tokens;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<SafetyCheckDto> Handle(CreateSafetyCheckCommand request, CancellationToken ct)
    {
        var userId = _currentUser.RequireUserId();

        var invitation = await _invitations.GetBySlugAsync(request.Slug, ct)
            ?? throw new InvitationNotFoundException(request.Slug);

        // Spec §7: the safety check lives on the confirmation screen, so it's
        // only available once the invitation is Accepted. Reject early.
        if (invitation.Status != InvitationStatus.Accepted)
            throw new InvalidSafetyCheckStateException("Safety check is only available after an invitation is accepted.");

        // If the caller already has an active (unconfirmed) check for this
        // invitation we return the existing one instead of creating duplicates.
        var existing = await _safetyChecks.GetActiveForInvitationAsync(invitation.Id, userId, ct);
        if (existing is not null)
        {
            return ToDto(existing);
        }

        var check = SafetyCheck.Create(
            invitation.Id,
            userId,
            _tokens.Generate(),
            invitation.MeetingAt);

        await _safetyChecks.AddAsync(check, ct);
        await _uow.CommitAsync(ct);

        return ToDto(check);
    }

    private static SafetyCheckDto ToDto(SafetyCheck s) =>
        new(s.FriendToken, s.ScheduledCheckInAt, s.ConfirmedAt, s.CreatedAt);
}

public class InvalidSafetyCheckStateException : Exception
{
    public InvalidSafetyCheckStateException(string message) : base(message) { }
}
