using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Application.Documents.Archive;

public enum DocumentArchiveOutcome
{
    Archived,
    Restored,
    NotFound,
    AlreadyArchived,
    NotArchived,
    ProcessingInProgress
}

public sealed record DocumentArchiveResult(
    DocumentArchiveOutcome Outcome,
    Guid? DocumentId = null,
    DocumentStatus? Status = null,
    bool IsArchived = false,
    DateTime? ArchivedAtUtc = null,
    string? ArchivedByDisplayName = null);
