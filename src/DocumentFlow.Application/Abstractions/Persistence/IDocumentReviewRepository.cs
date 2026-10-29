using DocumentFlow.Domain.Entities;

namespace DocumentFlow.Application.Abstractions.Persistence;

public interface IDocumentReviewRepository
{
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task PersistAsync(Document document, CancellationToken cancellationToken);
}
