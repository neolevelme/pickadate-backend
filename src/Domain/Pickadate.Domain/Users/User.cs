using Pickadate.BuildingBlocks.Domain;

namespace Pickadate.Domain.Users;

// Skeleton. Full implementation arrives in Faza 1 (auth flow).
public class User : Entity
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = null!;
    public string? Name { get; private set; }
    public string? Country { get; private set; }
    public string? VibePreference { get; private set; }
    public string? ProfileImageUrl { get; private set; }
    public UserRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private User() { }

    public static User Create(string email, string? name = null)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant(),
            Name = name,
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow
        };
    }
}
