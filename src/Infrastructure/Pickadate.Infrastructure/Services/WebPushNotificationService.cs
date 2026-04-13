using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pickadate.Application.Contracts;
using Pickadate.Domain.Notifications;
using WebPush;
using WebPushSubscription = WebPush.PushSubscription;

namespace Pickadate.Infrastructure.Services;

/// <summary>
/// VAPID-signed Web Push delivery. Uses the `WebPush` NuGet package —
/// it handles the ECDH + HKDF + AES-GCM dance. Stale subscriptions
/// (404 / 410 from the push service) are deleted eagerly so the next
/// sweep doesn't waste cycles on dead endpoints.
/// </summary>
public class WebPushNotificationService : INotificationService
{
    private readonly WebPushClient _client;
    private readonly IPushSubscriptionRepository _subscriptions;
    private readonly PushOptions _options;
    private readonly ILogger<WebPushNotificationService> _logger;

    public WebPushNotificationService(
        IPushSubscriptionRepository subscriptions,
        IOptions<PushOptions> options,
        ILogger<WebPushNotificationService> logger)
    {
        _subscriptions = subscriptions;
        _options = options.Value;
        _logger = logger;

        _client = new WebPushClient();
        _client.SetVapidDetails(_options.Subject, _options.PublicKey, _options.PrivateKey);
    }

    public async Task NotifyUsersAsync(IReadOnlyCollection<Guid> userIds, NotificationPayload payload, CancellationToken ct = default)
    {
        if (userIds.Count == 0) return;

        var subs = await _subscriptions.GetForUsersAsync(userIds, ct);
        if (subs.Count == 0) return;

        var json = JsonSerializer.Serialize(new
        {
            title = payload.Title,
            body = payload.Body,
            url = payload.Url,
            tag = payload.Tag
        });

        foreach (var sub in subs)
        {
            ct.ThrowIfCancellationRequested();
            var endpoint = sub.Endpoint;
            try
            {
                var webPushSub = new WebPushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                await _client.SendNotificationAsync(webPushSub, json);
            }
            catch (WebPushException ex) when (
                ex.StatusCode == HttpStatusCode.NotFound || ex.StatusCode == HttpStatusCode.Gone)
            {
                // Subscription has been removed on the client side. Drop it
                // so future sweeps don't keep trying.
                _logger.LogInformation("Dropping stale push subscription {Endpoint}", endpoint);
                await _subscriptions.DeleteByEndpointAsync(endpoint, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deliver push to {Endpoint}", endpoint);
            }
        }
    }

    public Task NotifyUserAsync(Guid userId, NotificationPayload payload, CancellationToken ct = default) =>
        NotifyUsersAsync(new[] { userId }, payload, ct);
}
