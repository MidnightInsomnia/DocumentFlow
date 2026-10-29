namespace DocumentFlow.Application.Documents.Files;

public sealed record DocumentFileResult(
    Stream Content,
    string ContentType,
    string FileName);
