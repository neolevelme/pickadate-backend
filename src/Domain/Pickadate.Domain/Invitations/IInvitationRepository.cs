namespace Pickadate.Domain.Invitations;

public interface IInvitationRepository
{
    Task<Invitation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Invitation?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);
    Task AddAsync(Invitation invitation, CancellationToken ct = default);

    /// <summary>Returns the invitations created by a user, newest first.</summary>
    Task<IReadOnlyList<Invitation>> ListForInitiatorAsync(Guid initiatorId, CancellationToken ct = default);

    /// <summary>Returns invitations whose owner-token hash is in the supplied set (anonymous only).</summary>
    Task<IReadOnlyList<Invitation>> FindByOwnerTokenHashesAsync(IReadOnlyCollection<string> hashes, CancellationToken ct = default);

    /// <summary>Removes invitations whose meeting is more than 30 days in the past.</summary>
    Task<int> PurgeOlderThanAsync(DateTime cutoffUtc, CancellationToken ct = default);
}
