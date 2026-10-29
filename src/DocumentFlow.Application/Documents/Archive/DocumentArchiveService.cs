using DocumentFlow.Application.Abstractions.Persistence;
using DocumentFlow.Application.Abstractions.Users;

namespace DocumentFlow.Application.Documents.Archive;

public sealed class DocumentArchiveService(
    IDocumentArchiveRepository repository,
    ICurrentUserContext currentUser,
    TimeProvider timeProvider)
{
    public Task<DocumentArchiveResult> ArchiveAsync(Guid id, CancellationToken cancellationToken) =>
        ChangeAsync(id, archive: true, cancellationToken);

    public Task<DocumentArchiveResult> RestoreAsync(Guid id, CancellationToken cancellationToken) =>
        ChangeAsync(id, archive: false, cancellationToken);

    private Task<DocumentArchiveResult> ChangeAsync(Guid id, bool archive, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not { } actorId ||
            string.IsNullOrWhiteSpace(currentUser.DisplayName))
            throw new InvalidOperationException("An authenticated user is required to change archive state.");

        return repository.ChangeAsync(id, archive, actorId, currentUser.DisplayName,
            timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
    }
}
