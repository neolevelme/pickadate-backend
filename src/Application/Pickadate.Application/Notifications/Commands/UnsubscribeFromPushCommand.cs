using MediatR;
using Pickadate.BuildingBlocks.Application;
using Pickadate.Domain.Notifications;

namespace Pickadate.Application.Notifications.Commands;

public record UnsubscribeFromPushCommand(string Endpoint) : ICommand;

public class UnsubscribeFromPushCommandHandler : IRequestHandler<UnsubscribeFromPushCommand>
{
    private readonly IPushSubscriptionRepository _subscriptions;
    private readonly IUnitOfWork _uow;

    public UnsubscribeFromPushCommandHandler(IPushSubscriptionRepository subscriptions, IUnitOfWork uow)
    {
        _subscriptions = subscriptions;
        _uow = uow;
    }

    public async Task Handle(UnsubscribeFromPushCommand request, CancellationToken ct)
    {
        // Idempotent: missing endpoints are a no-op, so the client can blindly
        // call this when clearing state without checking first.
        await _subscriptions.DeleteByEndpointAsync(request.Endpoint, ct);
        await _uow.CommitAsync(ct);
    }
}
