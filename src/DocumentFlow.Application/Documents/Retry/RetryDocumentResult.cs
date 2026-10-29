using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Application.Documents.Retry;

public sealed record RetryDocumentResult(
    RetryDocumentOutcome Outcome,
    Guid? DocumentId = null,
    DocumentStatus? Status = null,
    string? ErrorMessage = null);
