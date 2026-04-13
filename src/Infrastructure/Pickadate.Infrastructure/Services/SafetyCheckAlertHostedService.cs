using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pickadate.Application.Contracts;
using Pickadate.BuildingBlocks.Application;
using Pickadate.Domain.Safety;

namespace Pickadate.Infrastructure.Services;

/// <summary>
/// Spec §7: once a safety check's scheduled check-in time passes without
/// the user pressing "all good", the system should alert the friend. We
/// don't store friend contact details (anonymous bearer link only), so
/// instead we ping the user themselves with a nudge to confirm — the
/// friend still sees the "Overdue" state on the public /safety/:token
/// page when they refresh.
/// </summary>
public class SafetyCheckAlertHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    private readonly IServiceProvider _services;
    private readonly ILogger<SafetyCheckAlertHostedService> _logger;

    public SafetyCheckAlertHostedService(IServiceProvider services, ILogger<SafetyCheckAlertHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunOnceAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "Safety check sweep failed"); }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ISafetyCheckRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var now = DateTime.UtcNow;
        var due = await repo.GetDueForAlertAsync(now, ct);
        if (due.Count == 0) return;

        foreach (var check in due)
        {
            await notifications.NotifyUserAsync(
                check.UserId,
                new NotificationPayload(
                    Title: "Are you okay? 💛",
                    Body: "Your pickadate.me safety check is overdue. Tap to confirm you're safe.",
                    Url: "/dashboard",
                    Tag: $"safety-overdue-{check.Id}"),
                ct);

            check.MarkAlerted();
        }

        await uow.CommitAsync(ct);
    }
}
