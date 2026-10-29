namespace DocumentFlow.Api.Responses.Documents;

public sealed record DocumentListResponse(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyCollection<DocumentListItemResponse> Items);

public sealed record DocumentListItemResponse(
    Guid Id,
    string FileName,
    string Status,
    string Type,
    decimal? ClassificationConfidence,
    string TargetDepartment,
    DateTime CreatedAtUtc,
    DateTime? ProcessedAtUtc,
    bool IsArchived,
    string? MatchReason = null,
    string? Number = null,
    string? Date = null,
    string? Inn = null,
    string? Amount = null,
    string? CustomerINN = null,
    string? ContractorINN = null,
    string? SupplierINN = null,
    string? BuyerINN = null);

public sealed record DocumentDetailsResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string Status,
    string Type,
    decimal? ClassificationConfidence,
    string TargetDepartment,
    string? ExtractedText,
    string? ProcessingError,
    DateTime CreatedAtUtc,
    DateTime? ProcessingStartedAtUtc,
    DateTime? ProcessedAtUtc,
    IReadOnlyCollection<DocumentFieldResponse> Fields,
    IReadOnlyCollection<DocumentEventResponse> Events,
    bool IsArchived,
    DateTime? ArchivedAtUtc,
    string? ArchivedByDisplayName,
    string Version = "");

public sealed record DocumentFieldResponse(
    Guid Id,
    string Name,
    string? Value,
    decimal? Confidence,
    bool WasManuallyEdited,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record DocumentEventResponse(
    string Type,
    string? Message,
    DateTime CreatedAtUtc,
    Guid? ActorUserId = null,
    string? ActorDisplayName = null);
