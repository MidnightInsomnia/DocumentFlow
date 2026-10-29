namespace DocumentFlow.Application.Documents.Processing;

public enum TextExtractionFailureReason
{
    NoTextLayer,
    InvalidDocument,
    UnsupportedFormat,
    OcrFailed
}
