namespace DocumentFlow.Application.Documents.Processing;

public sealed class TextExtractionException : Exception
{
    public TextExtractionException(
        TextExtractionFailureReason reason,
        string message)
        : base(message)
    {
        Reason = reason;
    }

    public TextExtractionException(
        TextExtractionFailureReason reason,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        Reason = reason;
    }

    public TextExtractionFailureReason Reason { get; }
}
