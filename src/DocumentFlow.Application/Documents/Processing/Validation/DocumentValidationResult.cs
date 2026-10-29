namespace DocumentFlow.Application.Documents.Processing.Validation;

public sealed record DocumentValidationResult(
    bool IsValid,
    IReadOnlyCollection<DocumentValidationIssue> Issues);
