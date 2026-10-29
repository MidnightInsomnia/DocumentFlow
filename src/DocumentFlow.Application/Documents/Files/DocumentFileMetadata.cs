namespace DocumentFlow.Application.Documents.Files;

public sealed record DocumentFileMetadata(
    Guid Id,
    string OriginalFileName,
    string StoredFileName,
    string ContentType);
