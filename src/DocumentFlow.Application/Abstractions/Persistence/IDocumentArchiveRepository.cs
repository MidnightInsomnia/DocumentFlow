using DocumentFlow.Application.Documents.Archive;

namespace DocumentFlow.Application.Abstractions.Persistence;

public interface IDocumentArchiveRepository
{
    Task<DocumentArchiveResult> ChangeAsync(
        Guid documentId,
        bool archive,
        Guid actorUserId,
        string actorDisplayName,
        DateTime utcNow,
        CancellationToken cancellationToken);
}
