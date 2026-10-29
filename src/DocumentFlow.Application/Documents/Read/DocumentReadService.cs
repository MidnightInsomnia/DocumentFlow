using DocumentFlow.Application.Abstractions.Persistence;

namespace DocumentFlow.Application.Documents.Read;

public sealed class DocumentReadService(IDocumentReadRepository repository)
{
    public Task<PagedDocumentResult> GetPageAsync(
        DocumentListQuery query,
        CancellationToken cancellationToken) =>
        repository.GetPageAsync(query, cancellationToken);

    public Task<DocumentDetails?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        repository.GetByIdAsync(id, cancellationToken);
}
