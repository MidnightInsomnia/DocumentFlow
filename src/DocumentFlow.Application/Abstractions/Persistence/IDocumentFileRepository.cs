using DocumentFlow.Application.Documents.Files;

namespace DocumentFlow.Application.Abstractions.Persistence;

public interface IDocumentFileRepository
{
    Task<DocumentFileMetadata?> GetMetadataAsync(
        Guid documentId,
        CancellationToken cancellationToken);
}
