using DocumentFlow.Application.Abstractions.Persistence;
using DocumentFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocumentFlow.Infrastructure.Persistence.Repositories;

public sealed class DocumentActionAuditRepository(DocumentFlowDbContext dbContext)
    : IDocumentActionAuditRepository
{
    public async Task RecordAsync(DocumentEvent documentEvent, CancellationToken cancellationToken)
    {
        if (!await dbContext.Documents.AsNoTracking()
            .AnyAsync(document => document.Id == documentEvent.DocumentId, cancellationToken))
        {
            return;
        }

        dbContext.DocumentEvents.Add(documentEvent);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
