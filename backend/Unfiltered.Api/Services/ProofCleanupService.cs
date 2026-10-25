using Microsoft.EntityFrameworkCore;
using Unfiltered.Api.Data;
using Unfiltered.Api.Models;

namespace Unfiltered.Api.Services;

/// <summary>Deletes proof files once a review has been decided for longer than the retention period.</summary>
public class ProofCleanupService(IServiceScopeFactory scopes, FileStore files, ILogger<ProofCleanupService> logger) : BackgroundService
{
    public const int RetentionDays = 90;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        do
        {
            try
            {
                await PurgeAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Proof cleanup failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task PurgeAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);

        var old = await db.Reviews
            .Where(r => r.Status != ReviewStatus.Pending
                        && (r.ProofFile != null || r.RejectionFile != null)
                        && (r.PublishedAt ?? r.CreatedAt) < cutoff)
            .ToListAsync(ct);

        foreach (var r in old)
        {
            files.Delete(r.ProofFile);
            files.Delete(r.RejectionFile);
            r.ProofFile = null;
            r.RejectionFile = null;
        }
        await db.SaveChangesAsync(ct);
        if (old.Count > 0) logger.LogInformation("Deleted proof files for {Count} reviews", old.Count);
    }
}
