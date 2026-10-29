namespace DocumentFlow.Domain.Enums;

public enum DocumentEventType
{
    DocumentUploaded,
    ProcessingStarted,
    TextExtracted,
    DocumentClassified,
    FieldsExtracted,
    DocumentRouted,
    ManualReviewRequired,
    ManuallyReviewed,
    ProcessingCompleted,
    ProcessingFailed,
    ProcessingRetryRequested,
    AiSuggestionRequested,
    DocumentArchived,
    DocumentRestored,
    DocumentReprocessingRequested,
    DocumentReopened,
    DocumentResolved
}
