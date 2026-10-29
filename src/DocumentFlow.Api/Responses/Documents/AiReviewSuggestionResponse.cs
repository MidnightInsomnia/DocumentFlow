namespace DocumentFlow.Api.Responses.Documents;

public sealed record AiReviewSuggestionResponse(
    string SuggestedType,
    decimal ClassificationConfidence,
    IReadOnlyCollection<AiReviewSuggestedFieldResponse> Fields);

public sealed record AiReviewSuggestedFieldResponse(string Name, string Value, decimal Confidence);
