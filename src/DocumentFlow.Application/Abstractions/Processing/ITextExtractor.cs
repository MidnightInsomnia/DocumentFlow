using DocumentFlow.Application.Documents.Processing;
using DocumentFlow.Domain.Entities;

namespace DocumentFlow.Application.Abstractions.Processing;

public interface ITextExtractor
{
    Task<TextExtractionResult> ExtractAsync(
        Document document,
        CancellationToken cancellationToken);
}
