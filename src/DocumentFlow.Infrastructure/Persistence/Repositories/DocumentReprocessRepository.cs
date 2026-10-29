using System.Data;
using DocumentFlow.Application.Abstractions.Files;
using DocumentFlow.Application.Abstractions.Persistence;
using DocumentFlow.Application.Abstractions.Users;
using DocumentFlow.Application.Documents.Reprocess;
using DocumentFlow.Domain.Entities;
using DocumentFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DocumentFlow.Infrastructure.Persistence.Repositories;

public sealed class DocumentReprocessRepository(
    DocumentFlowDbContext dbContext,
    IFileStorage storage,
    ICurrentUserContext? currentUser = null) : IDocumentReprocessRepository
{
    public async Task<ReprocessDocumentResult> TryReprocessAsync(Guid documentId, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted, cancellationToken);
        var document = (await dbContext.Documents.FromSqlInterpolated($"""
            SELECT * FROM "Documents" WHERE "Id" = {documentId} FOR UPDATE
            """).ToListAsync(cancellationToken)).SingleOrDefault();
        if (document is null)
            return new ReprocessDocumentResult(ReprocessDocumentOutcome.NotFound);
        if (document.IsArchived)
            return new ReprocessDocumentResult(ReprocessDocumentOutcome.Archived, document.Id, document.Status);
        if (document.Status is DocumentStatus.PendingProcessing or DocumentStatus.Processing)
            return new ReprocessDocumentResult(ReprocessDocumentOutcome.ProcessingInProgress, document.Id, document.Status);

        await dbContext.Entry(document).Collection(item => item.Fields).LoadAsync(cancellationToken);
        await dbContext.Entry(document).Collection(item => item.Events).LoadAsync(cancellationToken);
        if (document.Fields.Any(field => field.WasManuallyEdited) ||
            document.Events.Any(item => item.Type == DocumentEventType.ManuallyReviewed))
            return new ReprocessDocumentResult(ReprocessDocumentOutcome.ManualEdits, document.Id, document.Status);

        try
        {
            await using var original = await storage.OpenReadAsync(document.StoredFileName, cancellationToken);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return new ReprocessDocumentResult(ReprocessDocumentOutcome.SourceMissing, document.Id, document.Status);
        }

        document.Reprocess();
        document.Events.Add(new DocumentEvent(document.Id,
            DocumentEventType.DocumentReprocessingRequested,
            "Document reprocessing requested.", currentUser?.UserId, currentUser?.DisplayName));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new ReprocessDocumentResult(ReprocessDocumentOutcome.Queued, document.Id, document.Status);
    }
}
