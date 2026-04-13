using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pickadate.BuildingBlocks.Application;
using Pickadate.Domain.Safety;

namespace Pickadate.Infrastructure.Services;

/// <summary>
/// Spec §7: once a safety check's scheduled check-in time passes without
/// the user pressing "all good", the friend should be notified. Real push
/// delivery is a Phase 5 concern — for now this service finds due checks,
/// logs a warning that an alert would fire, and marks them as alerted so
/// we don't log them again.
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

        var now = DateTime.UtcNow;
        var due = await repo.GetDueForAlertAsync(now, ct);
        if (due.Count == 0) return;

        foreach (var check in due)
        {
            _logger.LogWarning(
                "Safety check {Id} overdue (scheduled for {Scheduled:u}, user {User}, invitation {Invitation}) — friend alert would fire here once Phase 5 notifications land",
                check.Id, check.ScheduledCheckInAt, check.UserId, check.InvitationId);
            check.MarkAlerted();
        }

        await uow.CommitAsync(ct);
    }
}
