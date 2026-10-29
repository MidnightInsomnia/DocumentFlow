using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Application.Documents.Upload;

public sealed record UploadDocumentResult(
    Guid Id,
    string OriginalFileName,
    DocumentStatus Status,
    DateTime CreatedAtUtc);
