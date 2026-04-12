namespace Pickadate.Application.Auth.Dtos;

public record AuthUserDto(Guid Id, string Email, string? Name, string Role);

public record AuthResponse(string Token, DateTime ExpiresAt, AuthUserDto User);
