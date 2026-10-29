namespace DocumentFlow.Api.Responses.Documents;

public sealed record DocumentArchiveResponse(
    Guid Id,
    string Status,
    bool IsArchived,
    DateTime? ArchivedAtUtc,
    string? ArchivedByDisplayName);
