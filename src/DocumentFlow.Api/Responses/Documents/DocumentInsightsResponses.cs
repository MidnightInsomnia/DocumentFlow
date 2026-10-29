namespace DocumentFlow.Api.Responses.Documents;

public sealed record RelatedDocumentResponse(
    Guid Id,
    string FileName,
    string Type,
    string Status,
    string? Number,
    string? Date,
    string? Amount,
    bool IsArchived);

public sealed record RelatedDocumentsResponse(
    Guid DocumentId,
    string Type,
    string? ReferenceNumber,
    IReadOnlyList<RelatedDocumentResponse> Items);

public sealed record BusinessDuplicateMatchKeyResponse(string Inn, string Number, string Date);

public sealed record PotentialDuplicateItemResponse(
    Guid Id,
    string FileName,
    string Type,
    string Status,
    string? Number,
    string? Date,
    string? Inn,
    string? Amount,
    bool IsArchived,
    DateTime CreatedAtUtc,
    bool? AmountDiffers);

public sealed record PotentialDuplicatesResponse(
    Guid SourceDocumentId,
    bool Supported,
    BusinessDuplicateMatchKeyResponse? MatchKey,
    string Reason,
    string? SourceAmount,
    IReadOnlyList<PotentialDuplicateItemResponse> Items)
{
    public string CheckStatus => !Supported ? "notApplicable" : MatchKey is null ? "insufficientData" : "checked";
    public IReadOnlyList<string> CheckedRules => MatchKey is null ? [] : ["invoiceInnNumberDate"];
}

public sealed record ConsistencyWarningResponse(
    string Code,
    string Message,
    Guid? RelatedDocumentId,
    string? ActValue,
    string? ContractValue);

public sealed record ConsistencyChecksResponse(
    Guid DocumentId,
    IReadOnlyList<ConsistencyWarningResponse> Warnings,
    string CheckStatus = "insufficientData",
    string? Reason = null,
    IReadOnlyList<string>? CheckedRules = null);

public sealed record CounterpartyDocumentResponse(
    Guid Id,
    string FileName,
    string Type,
    string Status,
    string? Number,
    string? Kpp,
    string? Amount,
    DateTime CreatedAtUtc,
    bool IsArchived,
    string? CustomerINN = null,
    string? ContractorINN = null,
    string? SupplierINN = null,
    string? BuyerINN = null);

public sealed record CounterpartyCardResponse(
    string Inn,
    IReadOnlyList<string> KnownKpps,
    int DocumentCount,
    IReadOnlyDictionary<string, int> CountsByType,
    IReadOnlyDictionary<string, int> CountsByStatus,
    int NeedsReviewCount,
    decimal KnownDocumentAmountTotal,
    int AmountDocumentCount,
    IReadOnlyList<CounterpartyDocumentResponse> LatestDocuments,
    IReadOnlyList<CounterpartyDocumentResponse> Documents,
    int Page,
    int PageSize)
{
    public string InnScope => "anyParty";
}
