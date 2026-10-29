using DocumentFlow.Application.Documents.Processing.Validation;
using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Application.Documents.Review;

public enum DocumentReviewContextOutcome
{
    Success,
    NotFound,
    NotReviewable
}

public sealed record DocumentReviewContextResult(
    DocumentReviewContextOutcome Outcome,
    Guid? DocumentId = null,
    DocumentType? Type = null,
    decimal? ClassificationConfidence = null,
    IReadOnlyCollection<DocumentValidationIssue>? Issues = null);
