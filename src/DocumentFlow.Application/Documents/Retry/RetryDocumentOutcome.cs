namespace DocumentFlow.Application.Documents.Retry;

public enum RetryDocumentOutcome
{
    Queued,
    NotFound,
    NotRetryable
}
