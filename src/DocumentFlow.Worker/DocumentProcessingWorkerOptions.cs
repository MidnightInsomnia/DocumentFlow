namespace DocumentFlow.Worker;

public sealed class DocumentProcessingWorkerOptions
{
    public const string SectionName = "DocumentProcessingWorker";

    public int PollIntervalSeconds { get; init; } = 3;
    public int FailureRetryDelaySeconds { get; init; } = 5;
    public int StaleProcessingTimeoutMinutes { get; init; } = 10;
    public int RecoveryCheckIntervalSeconds { get; init; } = 60;
    public int RecoveryBatchSize { get; init; } = 50;
}
