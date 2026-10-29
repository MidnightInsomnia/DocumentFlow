namespace DocumentFlow.Application.Documents.Processing.FieldExtraction;

public sealed record ExtractedDocumentField(
    string Name,
    string? Value,
    decimal Confidence);
