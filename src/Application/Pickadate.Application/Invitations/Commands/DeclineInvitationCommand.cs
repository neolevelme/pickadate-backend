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
    private readonly IUnitOfWork _uow;

    public DeclineInvitationCommandHandler(
        IInvitationRepository invitations,
        IDeclineRecordRepository declines,
        IClientContext client,
        IUnitOfWork uow)
    {
        _invitations = invitations;
        _declines = declines;
        _client = client;
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
    }
}

public class TooManyDeclinesException : Exception
{
    public TooManyDeclinesException() : base("Too many declines from this network in the last 24 hours.") { }
}
