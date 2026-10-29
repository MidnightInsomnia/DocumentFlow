using DocumentFlow.Application.Documents.Reprocess;

namespace DocumentFlow.Application.Abstractions.Persistence;

public interface IDocumentReprocessRepository
{
    Task<ReprocessDocumentResult> TryReprocessAsync(Guid documentId, CancellationToken cancellationToken);
}
