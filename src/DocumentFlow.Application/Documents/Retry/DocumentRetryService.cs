using DocumentFlow.Application.Abstractions.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocumentFlow.Application.Documents.Retry;

public sealed class DocumentRetryService(
    IDocumentRetryRepository repository,
    ILogger<DocumentRetryService>? logger = null)
{
    private readonly ILogger<DocumentRetryService> _logger =
        logger ?? NullLogger<DocumentRetryService>.Instance;

    public async Task<RetryDocumentResult> RetryAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var result = await repository.TryRetryAsync(
            documentId,
            cancellationToken);

        if (result.Outcome == RetryDocumentOutcome.Queued)
        {
            _logger.LogInformation(
                "Document {DocumentId} was queued for processing retry.",
                result.DocumentId);
        }

        return result;
    }
}
