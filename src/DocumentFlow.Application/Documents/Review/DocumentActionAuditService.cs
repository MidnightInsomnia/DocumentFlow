using DocumentFlow.Application.Abstractions.Persistence;
using DocumentFlow.Application.Abstractions.Users;
using DocumentFlow.Domain.Entities;
using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Application.Documents.Review;

public sealed class DocumentActionAuditService(
    IDocumentActionAuditRepository repository,
    ICurrentUserContext currentUser)
{
    public Task RecordAiSuggestionRequestedAsync(Guid documentId, CancellationToken cancellationToken) =>
        repository.RecordAsync(new DocumentEvent(documentId,
            DocumentEventType.AiSuggestionRequested,
            "AI suggestion requested.",
            currentUser.UserId,
            currentUser.DisplayName), cancellationToken);
}