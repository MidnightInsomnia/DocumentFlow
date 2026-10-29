using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Infrastructure.ReviewWork;

public sealed class ReviewDraft
{
    public Guid DocumentId { get; set; }
    public Guid UserId { get; set; }
    public DocumentType Type { get; set; }
    public string FieldsJson { get; set; } = "[]";
    public Guid BaseVersion { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

// Immutable snapshot of the completed result before reopening. Actor identifies who reopened it;
// PreviousReviewer identifies the author of the completed result, when it was manually reviewed.
public sealed class ReviewRevision
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid DocumentVersion { get; set; }
    public DocumentType Type { get; set; }
    public Department TargetDepartment { get; set; }
    public decimal? ClassificationConfidence { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public string FieldsJson { get; set; } = "[]";
    public Guid ActorUserId { get; set; }
    public string ActorDisplayName { get; set; } = "";
    public Guid? PreviousReviewerUserId { get; set; }
    public string? PreviousReviewerDisplayName { get; set; }
    public string Reason { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class ReviewDisposition
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid DocumentVersion { get; set; }
    public string Resolution { get; set; } = "";
    public string Reason { get; set; } = "";
    public Guid ActorUserId { get; set; }
    public string ActorDisplayName { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}
