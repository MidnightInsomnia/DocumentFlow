using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Domain.Entities;

public sealed class DocumentEvent
{
    private DocumentEvent()
    {
    }

    public DocumentEvent(
        Guid documentId,
        DocumentEventType type,
        string? message = null,
        Guid? actorUserId = null,
        string? actorDisplayName = null)
    {
        if (documentId == Guid.Empty)
        {
            throw new ArgumentException("Document id must not be empty.", nameof(documentId));
        }

        Id = Guid.NewGuid();
        DocumentId = documentId;
        Type = type;
        Message = message;
        ActorUserId = actorUserId;
        ActorDisplayName = actorDisplayName;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public DocumentEventType Type { get; private set; }
    public string? Message { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string? ActorDisplayName { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Document Document { get; private set; } = null!;
}
