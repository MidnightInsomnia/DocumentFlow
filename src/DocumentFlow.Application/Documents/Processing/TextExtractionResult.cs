namespace DocumentFlow.Application.Documents.Processing;

public sealed record TextExtractionResult(
    string Text,
    TextExtractionMethod Method);
