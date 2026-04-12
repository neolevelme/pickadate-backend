using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pickadate.Domain.Invitations;
using Pickadate.Infrastructure.Persistence;

namespace Pickadate.Infrastructure.Services;

/// <summary>
/// Daily background job that enforces spec §10 data minimization rules:
/// - Invitations with a meeting more than 30 days in the past are deleted
///   (Anniversary-mode invitations are exempted once Faza 9 introduces them)
/// - Anonymous decline records expire after 24h (spec §12)
/// - Unused verification codes expire after their TTL
/// </summary>
public class InvitationPurgeHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private static readonly TimeSpan InvitationRetention = TimeSpan.FromDays(30);

    private readonly IServiceProvider _services;
    private readonly ILogger<InvitationPurgeHostedService> _logger;

    public InvitationPurgeHostedService(IServiceProvider services, ILogger<InvitationPurgeHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait one minute after startup before the first run so migrations
        // and the initial request burst have settled.
        try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Purge run failed; will retry next cycle");
            }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var invitations = scope.ServiceProvider.GetRequiredService<IInvitationRepository>();
        var db = scope.ServiceProvider.GetRequiredService<PickadateDbContext>();

        var now = DateTime.UtcNow;

        var invitationsDeleted = await invitations.PurgeOlderThanAsync(now - InvitationRetention, ct);

        var declinesDeleted = await db.DeclineRecords
            .Where(d => d.CreatedAt < now.AddHours(-24))
            .ExecuteDeleteAsync(ct);

        var codesDeleted = await db.VerificationCodes
            .Where(c => c.ExpiresAt < now)
            .ExecuteDeleteAsync(ct);

        _logger.LogInformation(
            "Purge swept invitations={Invitations}, declineRecords={Declines}, verificationCodes={Codes}",
            invitationsDeleted, declinesDeleted, codesDeleted);
    }
}
