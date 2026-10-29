using System.Data;
using DocumentFlow.Application.Abstractions.Persistence;
using DocumentFlow.Domain.Entities;
using DocumentFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DocumentFlow.Infrastructure.Persistence.Repositories;

public sealed class DocumentProcessingRepository(DocumentFlowDbContext dbContext)
    : IDocumentProcessingRepository
{
    private const string ProcessingTimeoutMessage = "Document processing timed out.";

    public async Task<Document?> TryClaimNextPendingAsync(
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        var pendingStatus = DocumentStatus.PendingProcessing;
        var documents = await dbContext.Documents
            .FromSqlInterpolated($"""
                SELECT *
                FROM "Documents"
                WHERE "Status" = {pendingStatus}
                  AND "IsArchived" = FALSE
                ORDER BY "CreatedAtUtc", "Id"
                FOR UPDATE SKIP LOCKED
                LIMIT 1
                """)
            .ToListAsync(cancellationToken);

        var document = documents.SingleOrDefault();
        if (document is not null)
        {
            document.StartProcessing();
            document.Events.Add(new DocumentEvent(
                document.Id,
                DocumentEventType.ProcessingStarted,
                "Document processing started."));

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return document;
    }

    public async Task<int> RecoverStaleProcessingAsync(
        DateTime staleBeforeUtc,
        int batchSize,
        CancellationToken cancellationToken)
    {
        if (staleBeforeUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Stale threshold must be UTC.", nameof(staleBeforeUtc));
        }

        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize));
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        var processingStatus = DocumentStatus.Processing;
        var documents = await dbContext.Documents
            .FromSqlInterpolated($"""
                SELECT *
                FROM "Documents"
                WHERE "Status" = {processingStatus}
                  AND "IsArchived" = FALSE
                  AND (
                      "ProcessingStartedAtUtc" IS NULL
                      OR "ProcessingStartedAtUtc" < {staleBeforeUtc}
                  )
                ORDER BY "ProcessingStartedAtUtc", "Id"
                FOR UPDATE SKIP LOCKED
                LIMIT {batchSize}
                """)
            .ToListAsync(cancellationToken);

        foreach (var document in documents)
        {
            document.FailProcessing(ProcessingTimeoutMessage);
            document.Events.Add(new DocumentEvent(
                document.Id,
                DocumentEventType.ProcessingFailed,
                ProcessingTimeoutMessage));
        }

        if (documents.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return documents.Count;
    }

    public async Task PersistAsync(
        Document document,
        CancellationToken cancellationToken)
    {
        if (dbContext.Entry(document).State == EntityState.Detached)
        {
            throw new InvalidOperationException(
                "The document must be tracked by the current processing DbContext.");
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
