using Dapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ParagLog.Infrastructure.Persistence;

namespace ParagLog.Infrastructure.Storage;

/// <summary>
/// Weekly maintenance job: compares blob folders on disk against activities.id and reports
/// (logs) any orphans - a blob folder outside every activity's create/delete transaction, per
/// SPEC.md's write/delete ordering ("an orphan file is acceptable, a dangling reference is not").
/// This is a system-wide scan across all users, not a per-user query, so it goes straight to the
/// connection rather than through IActivityRepository.
/// </summary>
public sealed class OrphanSweeper(
    NpgsqlConnectionFactory connectionFactory, string blobsRoot, ILogger<OrphanSweeper> logger) : BackgroundService
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromDays(7);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(SweepInterval);
        do
        {
            try
            {
                await SweepOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Orphan sweep failed.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task SweepOnceAsync(CancellationToken ct)
    {
        var blobActivityIds = EnumerateBlobActivityIds();
        var dbActivityIds = await LoadActivityIdsAsync(ct);

        var orphans = FindOrphans(blobActivityIds, dbActivityIds);
        if (orphans.Count == 0)
        {
            logger.LogInformation("Orphan sweep: no orphaned blob folders found ({Total} checked).", blobActivityIds.Count);
            return;
        }

        logger.LogWarning(
            "Orphan sweep found {Count} blob folder(s) with no matching activity row: {Ids}",
            orphans.Count, string.Join(", ", orphans));
    }

    public static IReadOnlyList<Guid> FindOrphans(
        IReadOnlyCollection<Guid> blobActivityIds, IReadOnlyCollection<Guid> dbActivityIds) =>
        blobActivityIds.Except(dbActivityIds).ToList();

    private IReadOnlyList<Guid> EnumerateBlobActivityIds()
    {
        if (!Directory.Exists(blobsRoot))
            return [];

        var ids = new List<Guid>();
        foreach (var userDir in Directory.EnumerateDirectories(blobsRoot))
        {
            var activitiesDir = Path.Combine(userDir, "activities");
            if (!Directory.Exists(activitiesDir))
                continue;

            foreach (var activityDir in Directory.EnumerateDirectories(activitiesDir))
            {
                if (Guid.TryParse(Path.GetFileName(activityDir), out var id))
                    ids.Add(id);
            }
        }

        return ids;
    }

    private async Task<IReadOnlyList<Guid>> LoadActivityIdsAsync(CancellationToken ct)
    {
        await using var connection = await connectionFactory.CreateOpenAsync(ct);
        var ids = await connection.QueryAsync<Guid>(
            new CommandDefinition("SELECT id FROM activities", cancellationToken: ct));
        return ids.ToList();
    }
}
