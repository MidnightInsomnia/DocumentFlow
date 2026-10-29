namespace DocumentFlow.Application.Documents.Upload;

public sealed record UploadDocumentCommand(
    Stream Content,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes);
