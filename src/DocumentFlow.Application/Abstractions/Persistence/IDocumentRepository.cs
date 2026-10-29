using DocumentFlow.Domain.Entities;

namespace DocumentFlow.Application.Abstractions.Persistence;

public interface IDocumentRepository
{
    Task<Document?> FindByContentHashAsync(string contentHash, CancellationToken cancellationToken);
    Task AddAsync(Document document, CancellationToken cancellationToken);
}
