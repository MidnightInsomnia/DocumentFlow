using DocumentFlow.Application.Abstractions.Persistence;

namespace DocumentFlow.Application.Documents.Processing;

public sealed class DocumentProcessingRecoveryService(
    IDocumentProcessingRepository repository)
{
    public Task<int> RecoverAsync(
        DateTime utcNow,
        TimeSpan staleTimeout,
        int batchSize,
        CancellationToken cancellationToken)
    {
        if (utcNow.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Current time must be UTC.", nameof(utcNow));
        }

        if (staleTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(staleTimeout));
        }

        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize));
        }

        return repository.RecoverStaleProcessingAsync(
            utcNow - staleTimeout,
            batchSize,
            cancellationToken);
    }
}
