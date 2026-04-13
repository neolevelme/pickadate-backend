using Pickadate.BuildingBlocks.Domain;

namespace Pickadate.Domain.Notifications;

/// <summary>
/// A single Web Push endpoint a user has authorised for pickadate.me.
/// Each browser + each device registers its own — one user can easily
/// own several. Endpoint is globally unique, so unsubscribing by
/// endpoint is enough to clean up stale rows.
/// </summary>
public class PushSubscription : Entity
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Endpoint { get; private set; } = null!;
    public string P256dh { get; private set; } = null!;
    public string Auth { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    private PushSubscription() { }

    public static PushSubscription Create(Guid userId, string endpoint, string p256dh, string auth)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("Endpoint is required.", nameof(endpoint));
        if (string.IsNullOrWhiteSpace(p256dh))
            throw new ArgumentException("p256dh key is required.", nameof(p256dh));
        if (string.IsNullOrWhiteSpace(auth))
            throw new ArgumentException("auth secret is required.", nameof(auth));

        return new PushSubscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Endpoint = endpoint,
            P256dh = p256dh,
            Auth = auth,
            CreatedAt = DateTime.UtcNow
        };
    }
}

public interface IPushSubscriptionRepository
{
    Task AddAsync(PushSubscription subscription, CancellationToken ct = default);
    Task<PushSubscription?> GetByEndpointAsync(string endpoint, CancellationToken ct = default);
    Task<IReadOnlyList<PushSubscription>> GetForUserAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<PushSubscription>> GetForUsersAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct = default);
    Task DeleteByEndpointAsync(string endpoint, CancellationToken ct = default);
}
