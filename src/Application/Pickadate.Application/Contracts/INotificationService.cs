namespace Pickadate.Application.Contracts;

/// <summary>
/// High-level notification payload. Infrastructure serializes this into
/// whatever the transport needs — Web Push wants JSON in the push body.
/// </summary>
public record NotificationPayload(
    string Title,
    string Body,
    string? Url = null,
    string? Tag = null);

public interface INotificationService
{
    /// <summary>Fan out a notification to every push subscription owned by the given users.</summary>
    Task NotifyUsersAsync(IReadOnlyCollection<Guid> userIds, NotificationPayload payload, CancellationToken ct = default);

    /// <summary>Convenience overload for a single user.</summary>
    Task NotifyUserAsync(Guid userId, NotificationPayload payload, CancellationToken ct = default);
}
