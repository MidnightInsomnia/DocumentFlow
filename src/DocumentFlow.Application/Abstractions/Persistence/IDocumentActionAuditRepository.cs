using DocumentFlow.Domain.Entities;

namespace DocumentFlow.Application.Abstractions.Persistence;

public interface IDocumentActionAuditRepository
{
    Task RecordAsync(DocumentEvent documentEvent, CancellationToken cancellationToken);
}