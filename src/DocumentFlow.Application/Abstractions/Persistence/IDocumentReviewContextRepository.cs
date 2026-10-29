using DocumentFlow.Domain.Entities;

namespace DocumentFlow.Application.Abstractions.Persistence;

public interface IDocumentReviewContextRepository
{
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
