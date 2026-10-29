using DocumentFlow.Application.Abstractions.Persistence;
using DocumentFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocumentFlow.Infrastructure.Persistence.Repositories;

public sealed class DocumentReviewContextRepository(DocumentFlowDbContext dbContext)
    : IDocumentReviewContextRepository
{
    public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Documents
            .AsNoTracking()
            .Include(document => document.Fields)
            .SingleOrDefaultAsync(document => document.Id == id, cancellationToken);
}
