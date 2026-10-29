using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Application.Documents.Review.AI;

public sealed record AiReviewSuggestedField(string Name, string Value, decimal Confidence);

public sealed record AiReviewSuggestion(
    DocumentType SuggestedType,
    decimal ClassificationConfidence,
    IReadOnlyCollection<AiReviewSuggestedField> Fields);

public enum AiReviewSuggestionOutcome
{
    Success,
    NotFound,
    NotReviewable,
    MissingExtractedText,
    Disabled,
    UpstreamUnavailable,
    ModelUnavailable,
    TimedOut,
    InvalidUpstreamResponse
}

public sealed record AiReviewSuggestionResult(
    AiReviewSuggestionOutcome Outcome,
    AiReviewSuggestion? Suggestion = null,
    string? ErrorMessage = null);
