using DocumentFlow.Application.Documents.Read;

namespace DocumentFlow.Application.Abstractions.Persistence;

public interface IDocumentReadRepository
{
    Task<PagedDocumentResult> GetPageAsync(
        DocumentListQuery query,
        CancellationToken cancellationToken);

    Task<DocumentDetails?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);
}
