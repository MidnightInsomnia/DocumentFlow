namespace DocumentFlow.Api.Responses.Documents;

public sealed record ReviewDocumentResponse(
    Guid Id,
    string Status,
    string Type,
    string TargetDepartment);

public sealed record ReviewValidationFailedResponse(
    string Title,
    IReadOnlyCollection<ReviewValidationIssueResponse> Issues);

public sealed record ReviewValidationIssueResponse(
    string Code,
    string? FieldName,
    string Message);
