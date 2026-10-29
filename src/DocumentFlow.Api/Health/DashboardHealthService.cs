using System.Reflection;
using DocumentFlow.Infrastructure.Health;
using DocumentFlow.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DocumentFlow.Api.Health;

public interface IDashboardHealthSource
{
    Task<string> CheckPostgresAsync(CancellationToken ct);
    Task<WorkerHeartbeatSnapshot> CheckWorkerAsync(DateTime nowUtc, CancellationToken ct);
    Task<OcrHealthResult> CheckOcrAsync(CancellationToken ct);
    Task<AiHealthResult> CheckAiAsync(CancellationToken ct);
}

// The health check already registered for readiness is the only database connectivity probe.
public sealed class DashboardHealthSource(HealthCheckService checks, IServiceScopeFactory scopes) : IDashboardHealthSource
{
    public async Task<string> CheckPostgresAsync(CancellationToken ct)
    {
        var report = await checks.CheckHealthAsync(registration => registration.Name == "postgres", ct);
        return report.Entries.TryGetValue("postgres", out var entry) && entry.Status == HealthStatus.Healthy
            ? "Healthy" : "Unavailable";
    }

    public async Task<WorkerHeartbeatSnapshot> CheckWorkerAsync(DateTime nowUtc, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<WorkerHeartbeatStore>().GetSnapshotAsync(nowUtc, ct);
    }

    public async Task<OcrHealthResult> CheckOcrAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<OcrHealthProbe>().CheckAsync(ct);
    }

    public async Task<AiHealthResult> CheckAiAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<AiHealthProbe>().CheckAsync(ct);
    }
}

public sealed record DashboardWorkerHealth(string Status, int HealthyInstances, int StaleInstances);
public sealed record DashboardOcrHealth(string Status, IReadOnlyList<string> Languages,
    IReadOnlyList<string>? MissingLanguages = null, string? ErrorCode = null);
public sealed record DashboardAiHealth(string Status);
public sealed record DashboardHealthSnapshot(string Overall, string Api, string Postgresql,
    DashboardWorkerHealth Worker, DashboardOcrHealth Ocr, DashboardAiHealth Ai, string Version, string Runtime);

// Cache the role-neutral snapshot, never a response tailored to a caller's authorization.
public sealed class DashboardHealthService(IDashboardHealthSource source, TimeProvider clock)
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(20);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DashboardHealthSnapshot? _snapshot;
    private DateTimeOffset _expiresAt;

    public async Task<DashboardHealthSnapshot> GetAsync(CancellationToken ct)
    {
        if (_snapshot is { } cached && clock.GetUtcNow() < _expiresAt) return cached;
        await _gate.WaitAsync(ct);
        try
        {
            if (_snapshot is { } current && clock.GetUtcNow() < _expiresAt) return current;
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
            deadline.CancelAfter(TimeSpan.FromSeconds(8));
            var token = deadline.Token;
            var postgres = await SafeAsync(() => source.CheckPostgresAsync(token), "Unavailable", token, ct);
            var worker = postgres == "Healthy"
                ? await SafeAsync(() => source.CheckWorkerAsync(clock.GetUtcNow().UtcDateTime, token),
                    new WorkerHeartbeatSnapshot(WorkerHeartbeatStatus.Unknown, 0, 0), token, ct)
                : new WorkerHeartbeatSnapshot(WorkerHeartbeatStatus.Unknown, 0, 0);
            var workerStatus = postgres == "Healthy" ? worker.Status.ToString() : "Unavailable";
            var ocr = await SafeAsync(() => source.CheckOcrAsync(token), new OcrHealthResult(OcrHealthStatus.Unavailable, [], []), token, ct);
            var ai = await SafeAsync(() => source.CheckAiAsync(token), new AiHealthResult(AiHealthStatus.Unavailable), token, ct);
            var overall = postgres != "Healthy" || workerStatus == "Stale" || workerStatus == "Unavailable"
                ? "Critical"
                : workerStatus == "Unknown" || ocr.Status != OcrHealthStatus.Healthy ? "Degraded" : "Healthy";
            var version = typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? typeof(Program).Assembly.GetName().Version?.ToString() ?? "Unknown";
            var snapshot = new DashboardHealthSnapshot(overall, "Healthy", postgres,
                new DashboardWorkerHealth(workerStatus, worker.FreshInstanceCount, worker.StaleInstanceCount),
                new DashboardOcrHealth(ocr.Status.ToString(), ocr.AvailableLanguages.ToArray(),
                    ocr.MissingLanguages.ToArray(), ocr.ErrorCode), new DashboardAiHealth(ai.Status.ToString()), version,
                System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
            _snapshot = snapshot;
            _expiresAt = clock.GetUtcNow() + Ttl;
            return snapshot;
        }
        finally { _gate.Release(); }
    }

    private static async Task<T> SafeAsync<T>(Func<Task<T>> probe, T fallback, CancellationToken cancellationToken, CancellationToken requestCancellation)
    {
        try { return await probe().WaitAsync(cancellationToken); }
        catch (OperationCanceledException) when (requestCancellation.IsCancellationRequested) { throw; }
        catch (Exception) { return fallback; } // Never surface provider errors, paths, or credentials.
    }
}
