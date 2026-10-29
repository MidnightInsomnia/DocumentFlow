using DocumentFlow.Application.Abstractions.Persistence;
using DocumentFlow.Domain.Entities;
using DocumentFlow.Application.Documents.Upload;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DocumentFlow.Infrastructure.Persistence.Repositories;

public sealed class DocumentRepository(DocumentFlowDbContext dbContext)
    : IDocumentRepository
{
    public Task<Document?> FindByContentHashAsync(string contentHash, CancellationToken cancellationToken) =>
        dbContext.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.ContentHash == contentHash, cancellationToken);

    public async Task AddAsync(
        Document document,
        CancellationToken cancellationToken)
    {
        dbContext.Documents.Add(document);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "UX_Documents_ContentHash"
            })
        {
            // SaveChanges failed: the new document and its event must not be retried on this DbContext.
            foreach (var entry in dbContext.ChangeTracker.Entries().Where(entry =>
                ReferenceEquals(entry.Entity, document) ||
                entry.Entity is DocumentEvent documentEvent && documentEvent.DocumentId == document.Id).ToList())
                entry.State = EntityState.Detached;

            var existing = await FindByContentHashAsync(document.ContentHash!, cancellationToken)
                ?? throw new InvalidOperationException("The conflicting document could not be found.", exception);
            throw new DuplicateDocumentException(existing.Id, existing.OriginalFileName, document.ContentHash!, existing.IsArchived);
        }
    }
}
