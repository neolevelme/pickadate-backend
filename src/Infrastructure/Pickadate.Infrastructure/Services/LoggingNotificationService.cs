using Microsoft.Extensions.Logging;
using Pickadate.Application.Contracts;

namespace Pickadate.Infrastructure.Services;

/// <summary>
/// Dev fallback that logs notifications instead of delivering them. Used
/// whenever `Push:PublicKey` / `Push:PrivateKey` aren't configured — the
/// rest of the notification pipeline still runs so you can see in the
/// console exactly what would have been sent.
/// </summary>
public class LoggingNotificationService : INotificationService
{
    private readonly ILogger<LoggingNotificationService> _logger;
    public LoggingNotificationService(ILogger<LoggingNotificationService> logger) => _logger = logger;

    public Task NotifyUsersAsync(IReadOnlyCollection<Guid> userIds, NotificationPayload payload, CancellationToken ct = default)
    {
        foreach (var userId in userIds)
        {
            _logger.LogInformation(
                "[NOTIFY dev] user={User} title={Title} body={Body} url={Url} tag={Tag}",
                userId, payload.Title, payload.Body, payload.Url ?? "-", payload.Tag ?? "-");
        }
        return Task.CompletedTask;
    }

    public Task NotifyUserAsync(Guid userId, NotificationPayload payload, CancellationToken ct = default) =>
        NotifyUsersAsync(new[] { userId }, payload, ct);
}
