namespace DocumentFlow.Api.Responses.Documents;

public sealed record BatchUploadItemResponse(
    string FileName,
    string Status,
    Guid? DocumentId = null,
    Guid? ExistingDocumentId = null,
    string? ExistingFileName = null,
    string? Error = null,
    bool? ExistingIsArchived = null);

public sealed record BatchUploadResponse(IReadOnlyList<BatchUploadItemResponse> Items);
