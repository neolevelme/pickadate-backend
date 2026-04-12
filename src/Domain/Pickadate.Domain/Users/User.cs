using Pickadate.BuildingBlocks.Domain;

namespace Pickadate.Domain.Users;

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
    public DateTime? LastLoginAt { get; private set; }

    private User() { }

    public static User Create(string email)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void RecordLogin() => LastLoginAt = DateTime.UtcNow;

    public void UpdateProfile(string? name, string? country, string? vibePreference, string? profileImageUrl)
    {
        Name = name;
        Country = country;
        VibePreference = vibePreference;
        ProfileImageUrl = profileImageUrl;
    }
}
