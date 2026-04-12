namespace Pickadate.Application.Invitations.Dtos;

public record PlaceDto(
    string GooglePlaceId,
    string Name,
    string FormattedAddress,
    double Lat,
    double Lng);

public record InvitationDetailDto(
    string Slug,
    string Vibe,
    string? CustomVibe,
    PlaceDto Place,
    DateTime MeetingAt,
    string? Message,
    string? MediaUrl,
    string Status,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    string InitiatorName);

public record CreateInvitationResult(string Slug);
