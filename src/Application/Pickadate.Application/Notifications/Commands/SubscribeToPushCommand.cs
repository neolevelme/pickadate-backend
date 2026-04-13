using FluentValidation;
using MediatR;
using Pickadate.Application.Contracts;
using Pickadate.BuildingBlocks.Application;
using Pickadate.Domain.Notifications;

namespace Pickadate.Application.Notifications.Commands;

public record SubscribeToPushCommand(string Endpoint, string P256dh, string Auth) : ICommand;

public class SubscribeToPushCommandValidator : AbstractValidator<SubscribeToPushCommand>
{
    public SubscribeToPushCommandValidator()
    {
        RuleFor(x => x.Endpoint).NotEmpty().MaximumLength(2048);
        RuleFor(x => x.P256dh).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Auth).NotEmpty().MaximumLength(256);
    }
}

public class SubscribeToPushCommandHandler : IRequestHandler<SubscribeToPushCommand>
{
    private readonly IPushSubscriptionRepository _subscriptions;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _uow;

    public SubscribeToPushCommandHandler(
        IPushSubscriptionRepository subscriptions,
        ICurrentUser currentUser,
        IUnitOfWork uow)
    {
        _subscriptions = subscriptions;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task Handle(SubscribeToPushCommand request, CancellationToken ct)
    {
        var userId = _currentUser.RequireUserId();

        // Upsert semantics: if the browser already registered this endpoint
        // (under the same or a different user), replace it so we don't keep
        // sending to stale rows.
        var existing = await _subscriptions.GetByEndpointAsync(request.Endpoint, ct);
        if (existing is not null)
        {
            await _subscriptions.DeleteByEndpointAsync(request.Endpoint, ct);
        }

        var sub = PushSubscription.Create(userId, request.Endpoint, request.P256dh, request.Auth);
        await _subscriptions.AddAsync(sub, ct);
        await _uow.CommitAsync(ct);
    }
}
