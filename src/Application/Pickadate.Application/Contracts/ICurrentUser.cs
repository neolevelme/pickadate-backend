namespace Pickadate.Application.Contracts;

public interface ICurrentUser
{
    /// <summary>The authenticated user's id, or null if the request is anonymous.</summary>
    Guid? UserId { get; }

    /// <summary>Throws when the request is anonymous; use at the top of commands that require auth.</summary>
    Guid RequireUserId();
}
