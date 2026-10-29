namespace DocumentFlow.Application.Documents.Read;

public sealed record PagedDocumentResult(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyCollection<DocumentListItem> Items);
