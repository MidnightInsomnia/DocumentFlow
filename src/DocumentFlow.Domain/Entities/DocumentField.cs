namespace DocumentFlow.Domain.Entities;

public sealed class DocumentField
{
    private DocumentField()
    {
    }

    public DocumentField(
        Guid documentId,
        string name,
        string? value = null,
        decimal? confidence = null)
    {
        if (documentId == Guid.Empty)
        {
            throw new ArgumentException("Document id must not be empty.", nameof(documentId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (confidence is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(confidence),
                "Confidence must be between 0 and 1.");
        }

        Id = Guid.NewGuid();
        DocumentId = documentId;
        Name = name;
        Value = value;
        Confidence = confidence;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Value { get; private set; }
    public decimal? Confidence { get; private set; }
    public bool WasManuallyEdited { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Document Document { get; private set; } = null!;

    public void UpdateManually(string? value)
    {
        Value = value;
        WasManuallyEdited = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
