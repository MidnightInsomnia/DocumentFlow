using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Application.Documents.Read;

public sealed record CounterpartyDocumentItem(
    Guid Id,
    string FileName,
    DocumentType Type,
    DocumentStatus Status,
    string? Number,
    string? Kpp,
    string? Amount,
    DateTime CreatedAtUtc,
    bool IsArchived,
    string? CustomerINN = null,
    string? ContractorINN = null,
    string? SupplierINN = null,
    string? BuyerINN = null);

public sealed record CounterpartyCard(
    string Inn,
    IReadOnlyList<string> KnownKpps,
    int DocumentCount,
    IReadOnlyDictionary<DocumentType, int> CountsByType,
    IReadOnlyDictionary<DocumentStatus, int> CountsByStatus,
    int NeedsReviewCount,
    decimal KnownDocumentAmountTotal,
    int AmountDocumentCount,
    IReadOnlyList<CounterpartyDocumentItem> LatestDocuments,
    IReadOnlyList<CounterpartyDocumentItem> Documents,
    int Page,
    int PageSize);
