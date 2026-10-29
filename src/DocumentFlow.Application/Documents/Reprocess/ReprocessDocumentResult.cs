using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Application.Documents.Reprocess;

public enum ReprocessDocumentOutcome
{
    Queued,
    NotFound,
    Archived,
    ProcessingInProgress,
    ManualEdits,
    SourceMissing
}

public sealed record ReprocessDocumentResult(
    ReprocessDocumentOutcome Outcome,
    Guid? DocumentId = null,
    DocumentStatus? Status = null);
