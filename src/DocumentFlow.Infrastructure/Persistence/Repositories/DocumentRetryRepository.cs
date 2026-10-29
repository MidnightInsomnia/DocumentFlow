using System.Data;
using DocumentFlow.Application.Abstractions.Persistence;
using DocumentFlow.Application.Abstractions.Users;
using DocumentFlow.Application.Documents.Retry;
using DocumentFlow.Domain.Entities;
using DocumentFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DocumentFlow.Infrastructure.Persistence.Repositories;

public sealed class DocumentRetryRepository(DocumentFlowDbContext dbContext,
    ICurrentUserContext? currentUser = null)
    : IDocumentRetryRepository
{
    public async Task<RetryDocumentResult> TryRetryAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        var documents = await dbContext.Documents
            .FromSqlInterpolated($"""
                SELECT *
                FROM "Documents"
                WHERE "Id" = {documentId}
                FOR UPDATE
                """)
            .ToListAsync(cancellationToken);
        var document = documents.SingleOrDefault();

        if (document is null)
        {
            return new RetryDocumentResult(RetryDocumentOutcome.NotFound);
        }

        if (document.IsArchived)
        {
            return new RetryDocumentResult(
                RetryDocumentOutcome.NotRetryable,
                document.Id,
                document.Status,
                "Restore the archived document before retrying.");
        }

        if (document.Status != DocumentStatus.Failed)
        {
            return new RetryDocumentResult(
                RetryDocumentOutcome.NotRetryable,
                document.Id,
                document.Status,
                "Only failed documents can be retried.");
        }

        await dbContext.Entry(document)
            .Collection(item => item.Fields)
            .LoadAsync(cancellationToken);

        document.RetryProcessing();
        document.Events.Add(new DocumentEvent(
            document.Id,
            DocumentEventType.ProcessingRetryRequested,
            "Document processing retry requested.",
            currentUser?.UserId,
            currentUser?.DisplayName));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new RetryDocumentResult(
            RetryDocumentOutcome.Queued,
            document.Id,
            document.Status);
    }
}
