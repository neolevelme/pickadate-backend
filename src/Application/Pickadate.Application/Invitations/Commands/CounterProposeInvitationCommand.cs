using FluentValidation;
using MediatR;
using Pickadate.Application.Contracts;
using Pickadate.BuildingBlocks.Application;
using Pickadate.Domain.Invitations;

namespace Pickadate.Application.Invitations.Commands;

public record CounterProposeInvitationCommand(
    string Slug,
    DateTime? NewMeetingAtUtc,
    string? PlaceGoogleId,
    string? PlaceName,
    string? PlaceFormattedAddress,
    double? PlaceLat,
    double? PlaceLng) : ICommand;

public class CounterProposeInvitationCommandValidator : AbstractValidator<CounterProposeInvitationCommand>
{
    public CounterProposeInvitationCommandValidator()
    {
        RuleFor(x => x.Slug).NotEmpty();

        RuleFor(x => x)
            .Must(x => x.NewMeetingAtUtc.HasValue || HasPlace(x))
            .WithMessage("Counter-proposal must change time, place, or both.");

        When(x => x.NewMeetingAtUtc.HasValue, () =>
        {
            RuleFor(x => x.NewMeetingAtUtc!.Value)
                .GreaterThan(DateTime.UtcNow)
                .WithMessage("New meeting time must be in the future.");
        });

        When(HasPlace, () =>
        {
            RuleFor(x => x.PlaceName).NotEmpty().MaximumLength(256);
            RuleFor(x => x.PlaceFormattedAddress).NotEmpty().MaximumLength(512);
            RuleFor(x => x.PlaceLat!.Value).InclusiveBetween(-90, 90);
            RuleFor(x => x.PlaceLng!.Value).InclusiveBetween(-180, 180);
        });
    }

    private static bool HasPlace(CounterProposeInvitationCommand x) =>
        !string.IsNullOrWhiteSpace(x.PlaceName)
        || !string.IsNullOrWhiteSpace(x.PlaceFormattedAddress)
        || x.PlaceLat.HasValue
        || x.PlaceLng.HasValue;
}

public class CounterProposeInvitationCommandHandler : IRequestHandler<CounterProposeInvitationCommand>
{
    private readonly IInvitationRepository _invitations;
    private readonly ICounterProposalRepository _counterProposals;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notifications;
    private readonly IUnitOfWork _uow;

    public CounterProposeInvitationCommandHandler(
        IInvitationRepository invitations,
        ICounterProposalRepository counterProposals,
        ICurrentUser currentUser,
        INotificationService notifications,
        IUnitOfWork uow)
    {
        _invitations = invitations;
        _counterProposals = counterProposals;
        _currentUser = currentUser;
        _notifications = notifications;
        _uow = uow;
    }

    public async Task Handle(CounterProposeInvitationCommand request, CancellationToken ct)
    {
        var userId = _currentUser.RequireUserId();

        var invitation = await _invitations.GetBySlugAsync(request.Slug, ct)
            ?? throw new InvitationNotFoundException(request.Slug);

        Place? newPlace = null;
        if (!string.IsNullOrWhiteSpace(request.PlaceName) && request.PlaceLat.HasValue && request.PlaceLng.HasValue)
        {
            newPlace = Place.Create(
                request.PlaceGoogleId ?? "",
                request.PlaceName,
                request.PlaceFormattedAddress ?? "",
                request.PlaceLat.Value,
                request.PlaceLng.Value);
        }

        var counter = invitation.CounterPropose(userId, request.NewMeetingAtUtc, newPlace);
        await _counterProposals.AddAsync(counter, ct);
        await _uow.CommitAsync(ct);

        // Notify the other party. The counter can go either way: the
        // recipient counters the initiator's proposal, or the initiator
        // counter-counters the recipient's one. Target whichever side
        // didn't just act.
        var otherSideId = userId == invitation.InitiatorId
            ? invitation.RecipientId
            : invitation.InitiatorId;

        if (otherSideId is Guid target)
        {
            await _notifications.NotifyUserAsync(
                target,
                new NotificationPayload(
                    Title: "They suggested a change",
                    Body: $"Round {invitation.CounterRound}/{Invitation.MaxCounterRounds} — tap to review.",
                    Url: "/dashboard",
                    Tag: $"invitation-countered-{invitation.Slug}"),
                ct);
        }
    }
}
