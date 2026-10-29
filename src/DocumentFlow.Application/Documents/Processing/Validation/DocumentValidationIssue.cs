namespace DocumentFlow.Application.Documents.Processing.Validation;

public sealed record DocumentValidationIssue(
    string Code,
    string? FieldName,
    string Message);
