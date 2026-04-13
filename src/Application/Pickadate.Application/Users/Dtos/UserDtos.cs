namespace Pickadate.Application.Users.Dtos;

public record MeDto(
    Guid Id,
    string Email,
    string? Name,
    string? Country,
    string? VibePreference,
    string? ProfileImageUrl,
    string Role,
    bool AnniversaryEnabled,
    DateTime CreatedAt);
