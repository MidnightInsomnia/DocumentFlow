using DocumentFlow.Domain.Constants;
using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Application.Documents.Processing;

public static class DocumentFieldSchema
{
    private static readonly IReadOnlyCollection<string> InvoiceRequired =
    [
        DocumentFieldNames.Number,
        DocumentFieldNames.Date,
        DocumentFieldNames.Inn,
        DocumentFieldNames.Amount
    ];

    private static readonly IReadOnlyCollection<string> InvoiceOptional =
    [DocumentFieldNames.Kpp, DocumentFieldNames.SupplierInn, DocumentFieldNames.BuyerInn];

    private static readonly IReadOnlyCollection<string> ContractRequired =
    [DocumentFieldNames.Number, DocumentFieldNames.Date];

    private static readonly IReadOnlyCollection<string> ContractOptional =
    [DocumentFieldNames.Amount, DocumentFieldNames.Inn,
        DocumentFieldNames.CustomerInn, DocumentFieldNames.ContractorInn];

    private static readonly IReadOnlyCollection<string> ActRequired =
    [DocumentFieldNames.Number, DocumentFieldNames.Date, DocumentFieldNames.Amount];

    private static readonly IReadOnlyCollection<string> ActOptional =
    [DocumentFieldNames.RelatedContractNumber, DocumentFieldNames.Inn,
        DocumentFieldNames.CustomerInn, DocumentFieldNames.ContractorInn];

    // Spaces (including NBSP/narrow NBSP) are grouping separators; comma is a decimal separator.
    public static string NormalizeAmount(string value) =>
        string.Concat(value.Where(character => !char.IsWhiteSpace(character))).Replace(',', '.');

    public static IReadOnlyCollection<string> GetRequiredFieldNames(DocumentType type) => type switch
    {
        DocumentType.Invoice => InvoiceRequired,
        DocumentType.Contract => ContractRequired,
        DocumentType.Act => ActRequired,
        _ => []
    };

    public static IReadOnlyCollection<string> GetOptionalFieldNames(DocumentType type) => type switch
    {
        DocumentType.Invoice => InvoiceOptional,
        DocumentType.Contract => ContractOptional,
        DocumentType.Act => ActOptional,
        _ => []
    };

    public static IReadOnlyCollection<string> GetSupportedFieldNames(DocumentType type) =>
        [.. GetRequiredFieldNames(type), .. GetOptionalFieldNames(type)];
}
