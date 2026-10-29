using System.Data;
using DocumentFlow.Application.Abstractions.Persistence;
using DocumentFlow.Application.Documents.Archive;
using DocumentFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DocumentFlow.Infrastructure.Persistence.Repositories;

public sealed class DocumentArchiveRepository(DocumentFlowDbContext dbContext) : IDocumentArchiveRepository
{
    public async Task<DocumentArchiveResult> ChangeAsync(Guid documentId, bool archive,
        Guid actorUserId, string actorDisplayName, DateTime utcNow, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted, cancellationToken);
        var document = (await dbContext.Documents.FromSqlInterpolated($"""
            SELECT * FROM "Documents" WHERE "Id" = {documentId} FOR UPDATE
            """).ToListAsync(cancellationToken)).SingleOrDefault();

        if (document is null)
            return new DocumentArchiveResult(DocumentArchiveOutcome.NotFound);
        if (archive && document.IsArchived)
            return new DocumentArchiveResult(DocumentArchiveOutcome.AlreadyArchived, documentId, document.Status, true,
                document.ArchivedAtUtc, document.ArchivedByDisplayName);
        if (!archive && !document.IsArchived)
            return new DocumentArchiveResult(DocumentArchiveOutcome.NotArchived, documentId, document.Status);
        if (archive && document.Status is (DocumentStatus.PendingProcessing or DocumentStatus.Processing))
            return new DocumentArchiveResult(DocumentArchiveOutcome.ProcessingInProgress, documentId, document.Status);

        if (archive) document.Archive(actorUserId, actorDisplayName, utcNow);
        else document.Restore(actorUserId, actorDisplayName);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new DocumentArchiveResult(archive ? DocumentArchiveOutcome.Archived : DocumentArchiveOutcome.Restored,
            document.Id, document.Status, document.IsArchived, document.ArchivedAtUtc, document.ArchivedByDisplayName);
    }
}
