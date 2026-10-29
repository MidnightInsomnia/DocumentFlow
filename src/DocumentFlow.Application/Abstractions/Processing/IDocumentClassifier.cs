using DocumentFlow.Application.Documents.Processing.Classification;

namespace DocumentFlow.Application.Abstractions.Processing;

public interface IDocumentClassifier
{
    Task<DocumentClassificationResult> ClassifyAsync(
        string text,
        CancellationToken cancellationToken);
}
