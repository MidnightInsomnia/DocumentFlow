using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Application.Documents.Read;

public sealed record DocumentDetails(
    Guid Id,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    DocumentStatus Status,
    DocumentType Type,
    decimal? ClassificationConfidence,
    Department TargetDepartment,
    string? ExtractedText,
    string? ProcessingError,
    DateTime CreatedAtUtc,
    DateTime? ProcessingStartedAtUtc,
    DateTime? ProcessedAtUtc,
    IReadOnlyCollection<DocumentFieldDetails> Fields,
    IReadOnlyCollection<DocumentEventDetails> Events,
    bool IsArchived = false,
    DateTime? ArchivedAtUtc = null,
    string? ArchivedByDisplayName = null,
    string Version = "");

public sealed record DocumentFieldDetails(
    Guid Id,
    string Name,
    string? Value,
    decimal? Confidence,
    bool WasManuallyEdited,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record DocumentEventDetails(
    DocumentEventType Type,
    string? Message,
    DateTime CreatedAtUtc,
    Guid? ActorUserId = null,
    string? ActorDisplayName = null);
