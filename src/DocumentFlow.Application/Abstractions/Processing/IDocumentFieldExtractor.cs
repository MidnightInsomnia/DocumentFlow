using DocumentFlow.Application.Documents.Processing.FieldExtraction;
using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Application.Abstractions.Processing;

public interface IDocumentFieldExtractor
{
    Task<IReadOnlyCollection<ExtractedDocumentField>> ExtractAsync(
        DocumentType documentType,
        string text,
        CancellationToken cancellationToken);
}
