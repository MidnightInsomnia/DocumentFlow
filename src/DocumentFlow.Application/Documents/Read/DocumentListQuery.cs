using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Application.Documents.Read;

public sealed record DocumentListQuery(
    int Page,
    int PageSize,
    DocumentStatus? Status,
    DocumentType? Type,
    Department? Department,
    string? Search = null,
    DateTime? CreatedFromUtcInclusive = null,
    DateTime? CreatedToUtcExclusive = null,
    DocumentSortField SortBy = DocumentSortField.CreatedAt,
    DocumentSortDirection SortDirection = DocumentSortDirection.Descending,
    DocumentArchiveFilter Archive = DocumentArchiveFilter.Active);

public enum DocumentArchiveFilter
{
    Active,
    Archived,
    All
}

public enum DocumentSortField
{
    CreatedAt,
    FileName,
    Status,
    Type
}

public enum DocumentSortDirection
{
    Ascending,
    Descending
}
