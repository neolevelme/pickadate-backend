using Pickadate.Application.Contracts;
using Pickadate.Domain.Invitations;

namespace Pickadate.Application.Invitations.Authorization;

/// <summary>
/// Owner-side authorisation for invitations. The caller is allowed to
/// mutate (cancel / complete / accept-counter / claim) when either:
///
///   1. they are authenticated as the invitation's <c>InitiatorId</c>, or
///   2. they present a raw owner token that hashes to the invitation's
///      <c>OwnerTokenHash</c>.
///
/// Either path is enough — we don't require both. Anonymous creators
/// rely on path 2; users who later created an account rely on path 1.
/// </summary>
public interface IInvitationOwnerAuthorizer
{
    /// <summary>Returns true when the caller currently identifies as the owner of <paramref name="invitation"/>.</summary>
    bool IsOwner(Invitation invitation);

    /// <summary>Throws <see cref="UnauthorizedAccessException"/> when the caller can't prove ownership.</summary>
    void AssertOwns(Invitation invitation);
}

public class InvitationOwnerAuthorizer : IInvitationOwnerAuthorizer
{
    private readonly ICurrentUser _currentUser;
    private readonly IOwnerTokenContext _ownerTokenContext;
    private readonly IOwnerTokenGenerator _ownerTokens;

    public InvitationOwnerAuthorizer(
        ICurrentUser currentUser,
        IOwnerTokenContext ownerTokenContext,
        IOwnerTokenGenerator ownerTokens)
    {
        _currentUser = currentUser;
        _ownerTokenContext = ownerTokenContext;
        _ownerTokens = ownerTokens;
    }

    public bool IsOwner(Invitation invitation)
    {
        // Authenticated initiator wins immediately.
        if (invitation.InitiatorId is Guid initiatorId
            && _currentUser.UserId is Guid userId
            && initiatorId == userId)
        {
            return true;
        }

        // Bearer owner token: hash the header, compare to the stored hash.
        if (!string.IsNullOrEmpty(invitation.OwnerTokenHash)
            && !string.IsNullOrEmpty(_ownerTokenContext.RawToken))
        {
            var hash = _ownerTokens.Hash(_ownerTokenContext.RawToken);
            if (string.Equals(hash, invitation.OwnerTokenHash, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public void AssertOwns(Invitation invitation)
    {
        if (!IsOwner(invitation))
        {
            throw new UnauthorizedAccessException("You don't own this invitation.");
        }
    }
}
