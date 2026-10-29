using DocumentFlow.Application.Documents.Processing.Validation;
using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Application.Documents.Review;

public enum ReviewDocumentOutcome
{
    Completed,
    NotFound,
    NotReviewable,
    InvalidRequest,
    ValidationFailed,
    VersionConflict
}

public sealed record ReviewDocumentResult(
    ReviewDocumentOutcome Outcome,
    Guid? DocumentId = null,
    DocumentStatus? Status = null,
    DocumentType? Type = null,
    Department? TargetDepartment = null,
    IReadOnlyCollection<DocumentValidationIssue>? Issues = null,
    string? ErrorMessage = null,
    string? Version = null);
