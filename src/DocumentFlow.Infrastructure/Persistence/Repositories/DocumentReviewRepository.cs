using DocumentFlow.Application.Abstractions.Persistence;
using DocumentFlow.Application.Abstractions.Users;
using DocumentFlow.Application.Documents.Review;
using DocumentFlow.Domain.Entities;
using DocumentFlow.Infrastructure.ReviewWork;
using Microsoft.EntityFrameworkCore;

namespace DocumentFlow.Infrastructure.Persistence.Repositories;

public sealed class DocumentReviewRepository(DocumentFlowDbContext dbContext, ICurrentUserContext? currentUser = null)
    : IDocumentReviewRepository
{
    public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Documents.Include(document => document.Fields)
            .SingleOrDefaultAsync(document => document.Id == id, cancellationToken);

    public async Task PersistAsync(Document document, CancellationToken cancellationToken)
    {
        if (dbContext.Entry(document).State == EntityState.Detached)
            throw new InvalidOperationException("The document must be tracked by the current review DbContext.");

        // The document UPDATE includes original Version/IsArchived in its WHERE clause. Fields,
        // events and only this reviewer's draft are committed together or all rolled back.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            if (currentUser?.UserId is { } userId)
                await dbContext.Set<ReviewDraft>()
                    .Where(item => item.DocumentId == document.Id && item.UserId == userId)
                    .ExecuteDeleteAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new DocumentVersionConflictException(document.Id, exception);
        }
    }
}
