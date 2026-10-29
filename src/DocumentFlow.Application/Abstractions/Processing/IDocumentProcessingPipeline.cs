using DocumentFlow.Domain.Entities;

namespace DocumentFlow.Application.Abstractions.Processing;

public interface IDocumentProcessingPipeline
{
    Task ProcessAsync(Document document, CancellationToken cancellationToken);
}
