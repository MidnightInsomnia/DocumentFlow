using DocumentFlow.Application.Abstractions.Files;
using DocumentFlow.Application.Abstractions.Persistence;
using Microsoft.Extensions.Logging;

namespace DocumentFlow.Application.Documents.Files;

public sealed class DocumentFileService(
    IDocumentFileRepository repository,
    IFileStorage fileStorage,
    ILogger<DocumentFileService> logger)
{
    public async Task<DocumentFileResult?> GetAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var metadata = await repository.GetMetadataAsync(
            documentId,
            cancellationToken);

        if (metadata is null)
        {
            return null;
        }

        try
        {
            var content = await fileStorage.OpenReadAsync(
                metadata.StoredFileName,
                cancellationToken);

            return new DocumentFileResult(
                content,
                metadata.ContentType,
                metadata.OriginalFileName);
        }
        catch (Exception exception) when (
            exception is FileNotFoundException or DirectoryNotFoundException)
        {
            logger.LogWarning(
                "Stored file for document {DocumentId} was not found.",
                documentId);
            return null;
        }
    }
}
