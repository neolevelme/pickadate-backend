namespace Pickadate.Application.Safety.Dtos;

public record SafetyCheckDto(
    string FriendToken,
    DateTime ScheduledCheckInAt,
    DateTime? ConfirmedAt,
    DateTime CreatedAt);

public record FriendSafetyViewDto(
    string InitiatorName,
    string PlaceName,
    string PlaceFormattedAddress,
    double PlaceLat,
    double PlaceLng,
    DateTime MeetingAt,
    DateTime ScheduledCheckInAt,
    DateTime? ConfirmedAt,
    string Status);
