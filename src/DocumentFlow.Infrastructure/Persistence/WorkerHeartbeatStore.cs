using Microsoft.EntityFrameworkCore;

namespace DocumentFlow.Infrastructure.Persistence;

public enum WorkerHeartbeatStatus { Unknown, Healthy, Stale }

public sealed record WorkerHeartbeatSnapshot(
    WorkerHeartbeatStatus Status,
    int FreshInstanceCount,
    int StaleInstanceCount);

/// <summary>Persistent worker liveness; a heartbeat is fresh for 60 seconds.</summary>
public sealed class WorkerHeartbeatStore(DocumentFlowDbContext db)
{
    public static readonly TimeSpan FreshnessThreshold = TimeSpan.FromSeconds(60);

    public async Task RecordAsync(
        string instanceId, DateTime startedAtUtc, string version, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        if (instanceId.Length > 200 || version.Length > 100)
            throw new ArgumentOutOfRangeException(nameof(instanceId), "Heartbeat identity or version exceeds database limits.");
        if (startedAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("StartedAtUtc must be UTC.", nameof(startedAtUtc));

        var lastSeenUtc = DateTime.UtcNow;
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "WorkerHeartbeats" ("InstanceId", "StartedAtUtc", "LastSeenUtc", "Version")
            VALUES ({instanceId}, {startedAtUtc}, {lastSeenUtc}, {version})
            ON CONFLICT ("InstanceId") DO UPDATE SET
                "StartedAtUtc" = EXCLUDED."StartedAtUtc",
                "LastSeenUtc" = EXCLUDED."LastSeenUtc",
                "Version" = EXCLUDED."Version"
            """, ct);
    }

    public async Task<WorkerHeartbeatSnapshot> GetSnapshotAsync(DateTime nowUtc, CancellationToken ct = default)
    {
        if (nowUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("NowUtc must be UTC.", nameof(nowUtc));
        var cutoff = nowUtc - FreshnessThreshold;
        // The aggregate stays on PostgreSQL; no per-instance rows are loaded into the API.
        var counts = await db.WorkerHeartbeats.AsNoTracking()
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Fresh = group.Count(heartbeat => heartbeat.LastSeenUtc >= cutoff),
                Stale = group.Count(heartbeat => heartbeat.LastSeenUtc < cutoff)
            })
            .SingleOrDefaultAsync(ct);
        var fresh = counts?.Fresh ?? 0;
        var stale = counts?.Stale ?? 0;
        return new WorkerHeartbeatSnapshot(
            fresh > 0 ? WorkerHeartbeatStatus.Healthy : stale > 0 ? WorkerHeartbeatStatus.Stale : WorkerHeartbeatStatus.Unknown,
            fresh, stale);
    }
}
