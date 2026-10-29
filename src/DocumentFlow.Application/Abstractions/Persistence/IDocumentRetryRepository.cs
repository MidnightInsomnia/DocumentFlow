using DocumentFlow.Application.Documents.Retry;

namespace DocumentFlow.Application.Abstractions.Persistence;

public interface IDocumentRetryRepository
{
    Task<RetryDocumentResult> TryRetryAsync(
        Guid documentId,
        CancellationToken cancellationToken);
}
