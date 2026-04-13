using FluentValidation;
using MediatR;
using Pickadate.Application.Contracts;
using Pickadate.BuildingBlocks.Application;
using Pickadate.Domain.AntiAbuse;
using Pickadate.Domain.Invitations;

namespace Pickadate.Application.Invitations.Commands;

public record DeclineInvitationCommand(string Slug, string? Reason) : ICommand;

public class DeclineInvitationCommandValidator : AbstractValidator<DeclineInvitationCommand>
{
    public DeclineInvitationCommandValidator()
    {
        RuleFor(x => x.Slug).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(80).When(x => !string.IsNullOrWhiteSpace(x.Reason));
    }
}

public class DeclineInvitationCommandHandler : IRequestHandler<DeclineInvitationCommand>
{
    // Spec §13: max 20 declines per IP per 24h.
    private const int DailyLimit = 20;

    private readonly IInvitationRepository _invitations;
    private readonly IDeclineRecordRepository _declines;
    private readonly IClientContext _client;
    private readonly INotificationService _notifications;
    private readonly IUnitOfWork _uow;

    public DeclineInvitationCommandHandler(
        IInvitationRepository invitations,
        IDeclineRecordRepository declines,
        IClientContext client,
        INotificationService notifications,
        IUnitOfWork uow)
    {
        _invitations = invitations;
        _declines = declines;
        _client = client;
        _notifications = notifications;
        _uow = uow;
    }

    public async Task Handle(DeclineInvitationCommand request, CancellationToken ct)
    {
        var ip = _client.Ip;

        var count = await _declines.CountInLast24hAsync(ip, ct);
        if (count >= DailyLimit)
        {
            throw new TooManyDeclinesException();
        }

        var invitation = await _invitations.GetBySlugAsync(request.Slug, ct)
            ?? throw new InvitationNotFoundException(request.Slug);

        invitation.Decline(request.Reason);

        await _declines.AddAsync(DeclineRecord.Create(ip), ct);
        await _uow.CommitAsync(ct);

        // Spec §4 Option 3 — deliberately calm tone, no comment echoed
        // in the push itself (the initiator sees the note when they open
        // the invitation). Anonymous initiators don't receive a push.
        if (invitation.InitiatorId is Guid initiatorId)
        {
            await _notifications.NotifyUserAsync(
                initiatorId,
                new NotificationPayload(
                    Title: "Your invitation wasn't accepted",
                    Body: "No worries — tap for details.",
                    Url: "/dashboard",
                    Tag: $"invitation-declined-{invitation.Slug}"),
                ct);
        }
    }
}

public class TooManyDeclinesException : Exception
{
    public TooManyDeclinesException() : base("Too many declines from this network in the last 24 hours.") { }
}
