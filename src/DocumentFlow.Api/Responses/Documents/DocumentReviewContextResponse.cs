namespace DocumentFlow.Api.Responses.Documents;

public sealed record DocumentReviewContextResponse(
    Guid Id,
    string Type,
    decimal? ClassificationConfidence,
    IReadOnlyList<ReviewValidationIssueResponse> Issues);
