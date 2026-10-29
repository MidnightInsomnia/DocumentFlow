using DocumentFlow.Application.Abstractions.Persistence;
using DocumentFlow.Application.Abstractions.Processing;
using DocumentFlow.Domain.Entities;
using DocumentFlow.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocumentFlow.Application.Documents.Processing;

public sealed class DocumentProcessingService(
    IDocumentProcessingRepository repository,
    IDocumentProcessingPipeline pipeline,
    ILogger<DocumentProcessingService>? logger = null)
{
    private readonly ILogger<DocumentProcessingService> _logger =
        logger ?? NullLogger<DocumentProcessingService>.Instance;

    private const int MaximumProcessingErrorLength = 2000;

    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var document = await repository.TryClaimNextPendingAsync(cancellationToken);
        if (document is null)
        {
            return false;
        }

        _logger.LogInformation(
            "Document {DocumentId} claimed for processing.",
            document.Id);

        try
        {
            await pipeline.ProcessAsync(document, cancellationToken);
            if (document.Status == DocumentStatus.Processing)
            {
                document.CompleteProcessing();
                document.Events.Add(new DocumentEvent(
                    document.Id,
                    DocumentEventType.ProcessingCompleted,
                    "Document processing completed."));
            }

            await repository.PersistAsync(document, cancellationToken);

            if (document.Status == DocumentStatus.Completed)
            {
                _logger.LogInformation(
                    "Document {DocumentId} processing completed. Type: {DocumentType}, Department: {Department}.",
                    document.Id,
                    document.Type,
                    document.TargetDepartment);
            }
            else if (document.Status == DocumentStatus.NeedsReview)
            {
                _logger.LogInformation(
                    "Document {DocumentId} requires manual review. Type: {DocumentType}.",
                    document.Id,
                    document.Type);
            }

            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            if (document.Status == DocumentStatus.Processing)
            {
                var error = Truncate(
                    string.IsNullOrWhiteSpace(exception.Message)
                        ? "Document processing failed."
                        : exception.Message,
                    MaximumProcessingErrorLength);

                document.FailProcessing(error);
                document.Events.Add(new DocumentEvent(
                    document.Id,
                    DocumentEventType.ProcessingFailed,
                    "Document processing failed."));

                await repository.PersistAsync(document, cancellationToken);
            }

            throw;
        }
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
