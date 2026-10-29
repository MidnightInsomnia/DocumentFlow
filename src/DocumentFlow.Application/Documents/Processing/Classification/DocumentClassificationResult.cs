using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Application.Documents.Processing.Classification;

public sealed record DocumentClassificationResult
{
    public DocumentClassificationResult(DocumentType type, decimal confidence)
    {
        if (confidence is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(confidence),
                "Classification confidence must be between 0 and 1.");
        }

        Type = type;
        Confidence = confidence;
    }

    public DocumentType Type { get; }
    public decimal Confidence { get; }
}
