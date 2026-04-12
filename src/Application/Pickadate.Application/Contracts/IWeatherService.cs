namespace Pickadate.Application.Contracts;

/// <summary>
/// Daily forecast for a single (lat, lng, date) triple. All fields are
/// nullable because Open-Meteo can omit individual variables; consumers
/// should only render what's present.
/// </summary>
public record WeatherForecast(
    double? TemperatureMaxC,
    double? TemperatureMinC,
    double? PrecipitationMm,
    int? WeatherCode,
    string Description);

public interface IWeatherService
{
    /// <summary>
    /// Returns a daily forecast for the given coordinates and date, or null
    /// when the date is outside Open-Meteo's 7-day window or the upstream
    /// call fails. Implementations cache results to keep the outbound
    /// request count down.
    /// </summary>
    Task<WeatherForecast?> GetForecastAsync(double lat, double lng, DateTime dateUtc, CancellationToken ct = default);
}
