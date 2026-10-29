using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Domain.Entities;

public sealed class Document
{
    private Document()
    {
    }

    public Document(
        string originalFileName,
        string storedFileName,
        string contentType,
        long fileSizeBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(storedFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        if (fileSizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fileSizeBytes),
                "File size must be greater than zero.");
        }

        Id = Guid.NewGuid();
        OriginalFileName = originalFileName;
        StoredFileName = storedFileName;
        ContentType = contentType;
        FileSizeBytes = fileSizeBytes;
        Status = DocumentStatus.PendingProcessing;
        Type = DocumentType.Unknown;
        TargetDepartment = Department.Unassigned;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid Version { get; private set; } = Guid.NewGuid();
    public string OriginalFileName { get; private set; } = null!;
    public string StoredFileName { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public long FileSizeBytes { get; private set; }
    public string? ContentHash { get; private set; }
    public bool IsArchived { get; private set; }
    public DateTime? ArchivedAtUtc { get; private set; }
    public Guid? ArchivedByUserId { get; private set; }
    public string? ArchivedByDisplayName { get; private set; }
    public DocumentStatus Status { get; private set; }
    public DocumentType Type { get; private set; }
    public string? ExtractedText { get; private set; }
    public decimal? ClassificationConfidence { get; private set; }
    public Department TargetDepartment { get; private set; }
    public string? ProcessingError { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ProcessingStartedAtUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public ICollection<DocumentField> Fields { get; } = new List<DocumentField>();
    public ICollection<DocumentEvent> Events { get; } = new List<DocumentEvent>();

    public void SetContentHash(string contentHash)
    {
        ArgumentNullException.ThrowIfNull(contentHash);
        if (contentHash.Length != 64 || contentHash.Any(c => c is not (>= '0' and <= '9') and not (>= 'a' and <= 'f')))
            throw new ArgumentException("Content hash must be a 64-character lowercase SHA-256 hexadecimal value.", nameof(contentHash));
        ContentHash = contentHash;
    }

    public void Archive(Guid actorUserId, string actorDisplayName, DateTime archivedAtUtc)
    {
        if (IsArchived)
            throw new InvalidOperationException("Document is already archived.");
        if (Status is DocumentStatus.PendingProcessing or DocumentStatus.Processing)
            throw new InvalidOperationException("Document processing is in progress.");
        if (actorUserId == Guid.Empty)
            throw new ArgumentException("Actor user id is required.", nameof(actorUserId));
        ArgumentException.ThrowIfNullOrWhiteSpace(actorDisplayName);
        if (archivedAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Archive time must be UTC.", nameof(archivedAtUtc));

        IsArchived = true;
        Version = Guid.NewGuid();
        ArchivedAtUtc = archivedAtUtc;
        ArchivedByUserId = actorUserId;
        ArchivedByDisplayName = actorDisplayName;
        Events.Add(new DocumentEvent(Id, DocumentEventType.DocumentArchived,
            "Документ перемещён в архив.", actorUserId, actorDisplayName));
    }

    public void Restore(Guid actorUserId, string actorDisplayName)
    {
        if (!IsArchived)
            throw new InvalidOperationException("Document is not archived.");
        if (actorUserId == Guid.Empty)
            throw new ArgumentException("Actor user id is required.", nameof(actorUserId));
        ArgumentException.ThrowIfNullOrWhiteSpace(actorDisplayName);

        IsArchived = false;
        Version = Guid.NewGuid();
        ArchivedAtUtc = null;
        ArchivedByUserId = null;
        ArchivedByDisplayName = null;
        Events.Add(new DocumentEvent(Id, DocumentEventType.DocumentRestored,
            "Документ восстановлен из архива.", actorUserId, actorDisplayName));
    }

    public void StartProcessing()
    {
        EnsureStatus(DocumentStatus.PendingProcessing);
        Status = DocumentStatus.Processing;
        Version = Guid.NewGuid();
        ProcessingStartedAtUtc = DateTime.UtcNow;
    }

    public void SetExtractedText(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ExtractedText = text;
    }

    public void SetClassification(DocumentType type, decimal confidence)
    {
        if (type == DocumentType.Unknown)
        {
            throw new ArgumentException(
                "A classified document cannot have the Unknown type.",
                nameof(type));
        }

        ValidateConfidence(confidence, nameof(confidence));
        Type = type;
        ClassificationConfidence = confidence;
    }

    public void SetTypeManually(DocumentType type)
    {
        EnsureStatus(DocumentStatus.NeedsReview);

        if (type == DocumentType.Unknown)
        {
            throw new ArgumentException(
                "A manually selected document type cannot be Unknown.",
                nameof(type));
        }

        if (Type != type)
        {
            Type = type;
            ClassificationConfidence = null;
        }
    }

    public void SetTargetDepartment(Department department)
    {
        if (department == Department.Unassigned)
        {
            throw new ArgumentException(
                "Unassigned is the initial department and cannot be a routing target.",
                nameof(department));
        }

        TargetDepartment = department;
    }

    public void AddField(string name, string? value, decimal? confidence)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (Fields.Any(field => string.Equals(
                field.Name,
                name,
                StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"A document field named '{name}' already exists.");
        }

        Fields.Add(new DocumentField(Id, name, value, confidence));
    }

    public void SetFieldManually(string name, string? value)
    {
        EnsureStatus(DocumentStatus.NeedsReview);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var field = Fields.FirstOrDefault(item => string.Equals(
            item.Name,
            name,
            StringComparison.OrdinalIgnoreCase));

        if (field is null)
        {
            field = new DocumentField(Id, name);
            Fields.Add(field);
        }

        field.UpdateManually(value);
    }

    public void ClearFieldsForManualReview()
    {
        EnsureStatus(DocumentStatus.NeedsReview);
        Fields.Clear();
    }

    public void RemoveFieldForManualReview(string name)
    {
        EnsureStatus(DocumentStatus.NeedsReview);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var field = Fields.FirstOrDefault(item => string.Equals(
            item.Name,
            name,
            StringComparison.OrdinalIgnoreCase));
        if (field is not null)
        {
            Fields.Remove(field);
        }
    }

    public void MarkNeedsReview()
    {
        EnsureStatus(DocumentStatus.Processing);
        Status = DocumentStatus.NeedsReview;
        Version = Guid.NewGuid();
    }

    public void CompleteProcessing()
    {
        if (Status is not DocumentStatus.Processing and not DocumentStatus.NeedsReview)
        {
            throw new InvalidOperationException(
                $"Cannot complete a document with status {Status}.");
        }

        Status = DocumentStatus.Completed;
        Version = Guid.NewGuid();
        ProcessedAtUtc = DateTime.UtcNow;
    }

    public void FailProcessing(string error)
    {
        EnsureStatus(DocumentStatus.Processing);
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        Status = DocumentStatus.Failed;
        Version = Guid.NewGuid();
        ProcessingError = error;
        ProcessedAtUtc = DateTime.UtcNow;
    }

    public void Reprocess()
    {
        if (IsArchived)
            throw new InvalidOperationException("Restore the archived document before reprocessing.");
        if (Status is not (DocumentStatus.Completed or DocumentStatus.NeedsReview or DocumentStatus.Failed))
            throw new InvalidOperationException("Only terminal documents can be reprocessed.");
        if (Fields.Any(field => field.WasManuallyEdited) ||
            Events.Any(documentEvent => documentEvent.Type == DocumentEventType.ManuallyReviewed))
            throw new InvalidOperationException("Manual review changes must not be discarded by reprocessing.");

        ResetProcessingResults();
    }

    public void ReopenForReview(Guid actorUserId, string actorDisplayName, string reason)
    {
        if (IsArchived)
            throw new InvalidOperationException("Сначала восстановите документ из архива.");
        EnsureStatus(DocumentStatus.Completed);
        if (actorUserId == Guid.Empty)
            throw new ArgumentException("Не указан пользователь.", nameof(actorUserId));
        ArgumentException.ThrowIfNullOrWhiteSpace(actorDisplayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        Status = DocumentStatus.NeedsReview;
        Version = Guid.NewGuid();
        Events.Add(new DocumentEvent(Id, DocumentEventType.DocumentReopened,
            $"Документ возвращён на проверку. Причина: {reason}", actorUserId, actorDisplayName));
    }

    public void RetryProcessing()
    {
        EnsureStatus(DocumentStatus.Failed);
        ResetProcessingResults();
    }

    private void ResetProcessingResults()
    {
        Version = Guid.NewGuid();
        Status = DocumentStatus.PendingProcessing;
        Type = DocumentType.Unknown;
        ExtractedText = null;
        ClassificationConfidence = null;
        TargetDepartment = Department.Unassigned;
        ProcessingError = null;
        ProcessingStartedAtUtc = null;
        ProcessedAtUtc = null;
        Fields.Clear();
    }

    private void EnsureStatus(DocumentStatus expected)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException(
                $"Expected document status {expected}, but current status is {Status}.");
        }
    }

    private static void ValidateConfidence(decimal confidence, string parameterName)
    {
        if (confidence is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Confidence must be between 0 and 1.");
        }
    }
}
