using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Application.Documents.Read;

public sealed record BusinessDuplicateMatchKey(string Inn, string Number, string Date);

public sealed record PotentialDuplicateItem(
    Guid Id,
    string FileName,
    DocumentType Type,
    DocumentStatus Status,
    string? Number,
    string? Date,
    string? Inn,
    string? Amount,
    bool IsArchived,
    DateTime CreatedAtUtc,
    bool? AmountDiffers);

public sealed record PotentialDuplicateResult(
    Guid SourceDocumentId,
    bool Supported,
    BusinessDuplicateMatchKey? MatchKey,
    string Reason,
    string? SourceAmount,
    IReadOnlyList<PotentialDuplicateItem> Items)
{
    public string CheckStatus => !Supported ? "notApplicable" : MatchKey is null ? "insufficientData" : "checked";
    public IReadOnlyList<string> CheckedRules => MatchKey is null ? [] : ["invoiceInnNumberDate"];
}
