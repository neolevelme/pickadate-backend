using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Pickadate.Application.Contracts;

namespace Pickadate.Infrastructure.Services;

/// <summary>
/// Open-Meteo daily forecast client. No API key required. Responses are
/// cached in-memory keyed by (lat rounded, lng rounded, date) with a
/// 6h TTL per spec §6 — weather doesn't swing enough in less than that
/// to bother the upstream. Returns null for dates beyond 7 days.
/// </summary>
public class OpenMeteoWeatherService : IWeatherService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(6);
    private static readonly TimeSpan ForecastHorizon = TimeSpan.FromDays(7);

    private readonly HttpClient _http;
    private readonly ILogger<OpenMeteoWeatherService> _logger;

    // Static cache survives scoped DI so multiple requests amortise the upstream call.
    private static readonly ConcurrentDictionary<string, CacheEntry> Cache = new();

    public OpenMeteoWeatherService(HttpClient http, ILogger<OpenMeteoWeatherService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<WeatherForecast?> GetForecastAsync(double lat, double lng, DateTime dateUtc, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var targetDate = dateUtc.Date;
        var daysAhead = (targetDate - now.Date).TotalDays;

        // Open-Meteo's free daily endpoint goes out 7 days. Anything beyond
        // that isn't useful for a date reminder anyway.
        if (daysAhead < 0 || daysAhead > ForecastHorizon.TotalDays) return null;

        var key = CacheKey(lat, lng, targetDate);
        if (Cache.TryGetValue(key, out var entry) && entry.ExpiresAt > now)
        {
            return entry.Forecast;
        }

        try
        {
            var url = "https://api.open-meteo.com/v1/forecast"
                + $"?latitude={lat.ToString("0.###", CultureInfo.InvariantCulture)}"
                + $"&longitude={lng.ToString("0.###", CultureInfo.InvariantCulture)}"
                + $"&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum"
                + $"&start_date={targetDate:yyyy-MM-dd}"
                + $"&end_date={targetDate:yyyy-MM-dd}"
                + "&timezone=UTC";

            var resp = await _http.GetFromJsonAsync<OpenMeteoResponse>(url, ct);
            if (resp?.Daily is null || resp.Daily.Time.Length == 0) return null;

            var forecast = new WeatherForecast(
                TemperatureMaxC: resp.Daily.Temperature2mMax?.FirstOrDefault(),
                TemperatureMinC: resp.Daily.Temperature2mMin?.FirstOrDefault(),
                PrecipitationMm: resp.Daily.PrecipitationSum?.FirstOrDefault(),
                WeatherCode: resp.Daily.WeatherCode?.FirstOrDefault(),
                Description: WmoCodes.Describe(resp.Daily.WeatherCode?.FirstOrDefault()));

            Cache[key] = new CacheEntry(forecast, now + CacheTtl);
            return forecast;
        }
        catch (Exception ex)
        {
            // Never let weather failures break the invitation response.
            _logger.LogWarning(ex, "Open-Meteo fetch failed for ({Lat}, {Lng}) on {Date}", lat, lng, targetDate);
            return null;
        }
    }

    private static string CacheKey(double lat, double lng, DateTime date)
    {
        // Round to 3 decimals (~110m) so two places on the same street
        // share the cache entry.
        var rlat = Math.Round(lat, 3);
        var rlng = Math.Round(lng, 3);
        return $"{rlat:0.###}|{rlng:0.###}|{date:yyyy-MM-dd}";
    }

    private readonly record struct CacheEntry(WeatherForecast Forecast, DateTime ExpiresAt);

    private sealed class OpenMeteoResponse
    {
        public OpenMeteoDaily? Daily { get; set; }
    }

    private sealed class OpenMeteoDaily
    {
        public string[] Time { get; set; } = Array.Empty<string>();
        public double[]? Temperature2mMax { get; set; }
        public double[]? Temperature2mMin { get; set; }
        public double[]? PrecipitationSum { get; set; }
        public int[]? WeatherCode { get; set; }
    }
}

/// <summary>
/// WMO weather interpretation codes — same table Open-Meteo documents.
/// Only the plain-English description matters for the UI; the numeric
/// code rides along for clients that want to pick their own icon.
/// </summary>
internal static class WmoCodes
{
    public static string Describe(int? code) => code switch
    {
        0 => "Clear sky",
        1 => "Mostly clear",
        2 => "Partly cloudy",
        3 => "Overcast",
        45 or 48 => "Fog",
        51 or 53 or 55 => "Drizzle",
        56 or 57 => "Freezing drizzle",
        61 => "Light rain",
        63 => "Rain",
        65 => "Heavy rain",
        66 or 67 => "Freezing rain",
        71 => "Light snow",
        73 => "Snow",
        75 => "Heavy snow",
        77 => "Snow grains",
        80 or 81 or 82 => "Rain showers",
        85 or 86 => "Snow showers",
        95 => "Thunderstorm",
        96 or 99 => "Thunderstorm with hail",
        _ => "Unknown"
    };
}
