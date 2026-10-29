namespace DocumentFlow.Api.Responses.Documents;

public sealed record UploadDocumentResponse(
    Guid Id,
    string FileName,
    string Status,
    DateTime CreatedAtUtc);
