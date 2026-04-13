using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pickadate.Domain.Anniversaries;

namespace Pickadate.Infrastructure.Services;

/// <summary>
/// Spec §8: on the month+day of a couple's first successful date, both sides
/// receive a mirror notification. This service runs once a day, finds every
/// anniversary whose month+day matches today's UTC date, and logs where the
/// notification would fire. Real push delivery lands with Phase 5.
/// </summary>
public class AnniversaryDetectionHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    private readonly IServiceProvider _services;
    private readonly ILogger<AnniversaryDetectionHostedService> _logger;

    public AnniversaryDetectionHostedService(IServiceProvider services, ILogger<AnniversaryDetectionHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Give the app ~2 minutes on startup to settle before the first sweep.
        try { await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunOnceAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "Anniversary sweep failed"); }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IAnniversaryRepository>();

        var now = DateTime.UtcNow;
        var due = await repo.GetDueOnAsync(now.Month, now.Day, ct);
        if (due.Count == 0) return;

        foreach (var a in due)
        {
            var years = a.YearsSince(now);
            // Skip the same-day-as-creation case so we don't fire on year 0.
            if (years <= 0) continue;

            _logger.LogWarning(
                "[ANNIVERSARY] Pair ({A}, {B}) — {Years}y since first date ({FirstDate:yyyy-MM-dd}) — notification would fire here once Phase 5 notifications land",
                a.UserAId, a.UserBId, years, a.FirstDateAt);
        }
    }
}
