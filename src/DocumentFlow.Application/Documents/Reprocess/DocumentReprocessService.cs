using DocumentFlow.Application.Abstractions.Persistence;

namespace DocumentFlow.Application.Documents.Reprocess;

public sealed class DocumentReprocessService(IDocumentReprocessRepository repository)
{
    public Task<ReprocessDocumentResult> ReprocessAsync(Guid documentId, CancellationToken cancellationToken) =>
        repository.TryReprocessAsync(documentId, cancellationToken);
}
