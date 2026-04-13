using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pickadate.Application.Contracts;
using Pickadate.Domain.Invitations;
using Pickadate.Infrastructure.Persistence;

namespace Pickadate.Infrastructure.Services;

/// <summary>
/// Spec §9: fires the "meeting in 24h" and "meeting in 2h" reminders for
/// every accepted invitation. Runs every 30 minutes. Idempotency is
/// enforced via shadow columns `Reminder24hSentAt` and `Reminder2hSentAt`
/// on the invitation row — a given reminder fires at most once even if
/// the service restarts mid-sweep.
/// </summary>
public class InvitationReminderHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(30);

    // Window for the 24h reminder: meeting is between 23h and 25h away.
    // Wider than the tick interval so a single missed sweep doesn't skip it.
    private static readonly TimeSpan Reminder24hMin = TimeSpan.FromHours(23);
    private static readonly TimeSpan Reminder24hMax = TimeSpan.FromHours(25);

    // Window for the 2h reminder: between 1.5h and 2.5h.
    private static readonly TimeSpan Reminder2hMin = TimeSpan.FromHours(1.5);
    private static readonly TimeSpan Reminder2hMax = TimeSpan.FromHours(2.5);

    private readonly IServiceProvider _services;
    private readonly ILogger<InvitationReminderHostedService> _logger;

    public InvitationReminderHostedService(IServiceProvider services, ILogger<InvitationReminderHostedService> logger)
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
            catch (Exception ex) { _logger.LogError(ex, "Reminder sweep failed"); }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PickadateDbContext>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var now = DateTime.UtcNow;
        var windowEnd24h = now + Reminder24hMax;
        var windowStart24h = now + Reminder24hMin;
        var windowEnd2h = now + Reminder2hMax;
        var windowStart2h = now + Reminder2hMin;

        // 24h reminder — only Accepted invitations where we haven't sent it yet.
        var due24h = await db.Invitations
            .Where(i => i.Status == InvitationStatus.Accepted
                        && i.MeetingAt >= windowStart24h
                        && i.MeetingAt <= windowEnd24h
                        && EF.Property<DateTime?>(i, "Reminder24hSentAt") == null)
            .ToListAsync(ct);

        foreach (var invitation in due24h)
        {
            await SendReminderAsync(notifications, invitation, hoursAway: 24, ct);
            db.Entry(invitation).Property("Reminder24hSentAt").CurrentValue = now;
        }

        // 2h reminder.
        var due2h = await db.Invitations
            .Where(i => i.Status == InvitationStatus.Accepted
                        && i.MeetingAt >= windowStart2h
                        && i.MeetingAt <= windowEnd2h
                        && EF.Property<DateTime?>(i, "Reminder2hSentAt") == null)
            .ToListAsync(ct);

        foreach (var invitation in due2h)
        {
            await SendReminderAsync(notifications, invitation, hoursAway: 2, ct);
            db.Entry(invitation).Property("Reminder2hSentAt").CurrentValue = now;
        }

        if (due24h.Count > 0 || due2h.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }
    }

    private static Task SendReminderAsync(INotificationService notifications, Invitation invitation, int hoursAway, CancellationToken ct)
    {
        // Notify both sides when we have both — otherwise just the initiator.
        var recipients = invitation.RecipientId is Guid r
            ? new[] { invitation.InitiatorId, r }
            : new[] { invitation.InitiatorId };

        var title = hoursAway == 24 ? "Your meeting is tomorrow" : "Meeting in 2 hours";
        var body = $"{invitation.Place.Name} — {invitation.MeetingAt:HH:mm} UTC";

        return notifications.NotifyUsersAsync(
            recipients,
            new NotificationPayload(
                title,
                body,
                $"/i/{invitation.Slug}",
                $"reminder-{hoursAway}h-{invitation.Slug}"),
            ct);
    }
}
