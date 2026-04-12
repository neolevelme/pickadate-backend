namespace Pickadate.Domain.Invitations;

public interface IInvitationRepository
{
    Task<Invitation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Invitation?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);
    Task AddAsync(Invitation invitation, CancellationToken ct = default);
}
