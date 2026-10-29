namespace DocumentFlow.Infrastructure.Persistence;

public sealed class WorkerHeartbeat
{
    public string InstanceId { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime LastSeenUtc { get; set; }
    public string Version { get; set; } = string.Empty;
}
