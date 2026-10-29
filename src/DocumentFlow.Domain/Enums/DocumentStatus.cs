namespace DocumentFlow.Domain.Enums;

public enum DocumentStatus
{
    PendingProcessing,
    Processing,
    NeedsReview,
    Completed,
    Failed
}
