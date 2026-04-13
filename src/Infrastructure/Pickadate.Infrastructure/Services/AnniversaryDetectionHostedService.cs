using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pickadate.Application.Contracts;
using Pickadate.Domain.Anniversaries;

namespace Pickadate.Infrastructure.Services;

/// <summary>
/// Spec §8: on the month+day of a couple's first successful date, both sides
/// receive a mirror notification. This service runs once a day, finds every
/// anniversary whose month+day matches today's UTC date, and dispatches a
/// push to both halves of the pair.
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
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var now = DateTime.UtcNow;
        var due = await repo.GetDueOnAsync(now.Month, now.Day, ct);
        if (due.Count == 0) return;

        foreach (var a in due)
        {
            var years = a.YearsSince(now);
            // Skip the same-day-as-creation case so we don't fire on year 0.
            if (years <= 0) continue;

            var title = years == 1 ? "One year ago today ✨" : $"{years} years ago today ✨";
            var body = "Remember your first pickadate? Tap to send one back.";
            var tag = $"anniversary-{a.Id}-{now:yyyyMMdd}";

            await notifications.NotifyUsersAsync(
                new[] { a.UserAId, a.UserBId },
                new NotificationPayload(title, body, "/create", tag),
                ct);
        }
    }
}
