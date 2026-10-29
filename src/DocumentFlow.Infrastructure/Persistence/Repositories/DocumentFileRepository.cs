using DocumentFlow.Application.Abstractions.Persistence;
using DocumentFlow.Application.Documents.Files;
using Microsoft.EntityFrameworkCore;

namespace DocumentFlow.Infrastructure.Persistence.Repositories;

public sealed class DocumentFileRepository(DocumentFlowDbContext dbContext)
    : IDocumentFileRepository
{
    public Task<DocumentFileMetadata?> GetMetadataAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        return dbContext.Documents
            .AsNoTracking()
            .Where(document => document.Id == documentId)
            .Select(document => new DocumentFileMetadata(
                document.Id,
                document.OriginalFileName,
                document.StoredFileName,
                document.ContentType))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
