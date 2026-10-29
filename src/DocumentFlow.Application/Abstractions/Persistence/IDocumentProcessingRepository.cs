using DocumentFlow.Domain.Entities;

namespace DocumentFlow.Application.Abstractions.Persistence;

public interface IDocumentProcessingRepository
{
    Task<Document?> TryClaimNextPendingAsync(CancellationToken cancellationToken);

    Task<int> RecoverStaleProcessingAsync(
        DateTime staleBeforeUtc,
        int batchSize,
        CancellationToken cancellationToken);

    Task PersistAsync(Document document, CancellationToken cancellationToken);
}
