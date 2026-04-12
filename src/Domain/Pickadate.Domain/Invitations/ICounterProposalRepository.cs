namespace Pickadate.Domain.Invitations;

public interface ICounterProposalRepository
{
    Task AddAsync(CounterProposal counterProposal, CancellationToken ct = default);
    Task<CounterProposal?> GetLatestForInvitationAsync(Guid invitationId, CancellationToken ct = default);
}
