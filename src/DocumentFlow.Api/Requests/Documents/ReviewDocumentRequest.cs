namespace DocumentFlow.Api.Requests.Documents;

public sealed record ReviewDocumentRequest(
    string? Type,
    IReadOnlyCollection<ReviewDocumentFieldRequest>? Fields,
    string? ExpectedVersion = null);

public sealed record ReviewDocumentFieldRequest(string? Name, string? Value);
