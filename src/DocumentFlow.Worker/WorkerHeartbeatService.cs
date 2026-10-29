using DocumentFlow.Infrastructure.Persistence;

namespace DocumentFlow.Worker;

/// <summary>Emits one PostgreSQL heartbeat per running worker process every 15 seconds.</summary>
public sealed class WorkerHeartbeatService(
    IServiceScopeFactory scopeFactory,
    ILogger<WorkerHeartbeatService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);
    private readonly string _instanceId = Guid.NewGuid().ToString("N");
    private readonly DateTime _startedAtUtc = DateTime.UtcNow;
    private readonly string _version = typeof(WorkerHeartbeatService).Assembly.GetName().Version?.ToString() ?? "unknown";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        try
        {
            do
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    await scope.ServiceProvider.GetRequiredService<WorkerHeartbeatStore>()
                        .RecordAsync(_instanceId, _startedAtUtc, _version, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogWarning("Worker heartbeat write failed ({FailureType}); retrying on next tick.", exception.GetType().Name);
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host shutdown cancels the timer and any in-flight database write.
        }
    }
}
