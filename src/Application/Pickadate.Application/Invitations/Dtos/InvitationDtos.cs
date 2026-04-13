namespace Pickadate.Application.Invitations.Dtos;

public record PlaceDto(
    string GooglePlaceId,
    string Name,
    string FormattedAddress,
    double Lat,
    double Lng);

public record CounterProposalDto(
    int Round,
    string Kind,
    DateTime? NewMeetingAt,
    PlaceDto? NewPlace,
    DateTime CreatedAt);

public record WeatherDto(
    double? TemperatureMaxC,
    double? TemperatureMinC,
    double? PrecipitationMm,
    int? WeatherCode,
    string Description);

public record InvitationDetailDto(
    string Slug,
    string Vibe,
    string? CustomVibe,
    PlaceDto Place,
    DateTime MeetingAt,
    string? Message,
    string? MediaUrl,
    string Status,
    int CounterRound,
    int MaxCounterRounds,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    string InitiatorName,
    CounterProposalDto? LatestCounter,
    WeatherDto? Weather,
    bool ViewerIsOwner,
    bool HasAccount);

/// <summary>
/// Returned exactly once when an invitation is created. <see cref="OwnerToken"/>
/// is non-null only when the creator was anonymous — that's the recovery
/// code the browser stores under `pickadate.owner.&lt;slug&gt;`.
/// </summary>
public record CreateInvitationResult(string Slug, string? OwnerToken);
