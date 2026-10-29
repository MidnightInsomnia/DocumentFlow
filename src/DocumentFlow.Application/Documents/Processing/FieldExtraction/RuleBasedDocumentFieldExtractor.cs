using System.Globalization;
using System.Text.RegularExpressions;
using DocumentFlow.Application.Abstractions.Processing;
using DocumentFlow.Domain.Constants;
using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Application.Documents.Processing.FieldExtraction;

public sealed partial class RuleBasedDocumentFieldExtractor : IDocumentFieldExtractor
{
    private const decimal ExactLabelConfidence = 0.95m;
    private const decimal DateConfidence = 0.90m;
    private const decimal ActAmountConfidence = 0.88m;
    private const decimal ContractAmountConfidence = 0.92m;
    private const decimal RelatedContractConfidence = 0.93m;

    private static readonly string[] SupportedDateFormats =
    [
        "dd.MM.yyyy", "dd/MM/yyyy", "dd-MM-yyyy",
        "d.M.yyyy", "d/M/yyyy", "d-M-yyyy"
    ];

    public Task<IReadOnlyCollection<ExtractedDocumentField>> ExtractAsync(
        DocumentType documentType,
        string text,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(text);
        cancellationToken.ThrowIfCancellationRequested();

        if (documentType == DocumentType.Unknown)
        {
            return Task.FromResult<IReadOnlyCollection<ExtractedDocumentField>>([]);
        }

        var normalizedText = text
            .Replace('\u00A0', ' ')
            .Replace("\r\n", "\n")
            .Replace('\r', '\n');
        var fields = new List<ExtractedDocumentField>();

        AddMatch(fields, DocumentFieldNames.Number, GetNumberRegex(documentType), normalizedText,
            static value => value, ExactLabelConfidence);
        AddMatch(fields, DocumentFieldNames.Date, DateRegex(), normalizedText,
            NormalizeDate, DateConfidence);

        if (documentType == DocumentType.Invoice)
        {
            AddMatch(fields, DocumentFieldNames.Inn, InnRegex(), normalizedText,
                static value => value, ExactLabelConfidence);
            AddMatch(fields, DocumentFieldNames.Kpp, KppRegex(), normalizedText,
                static value => value, ExactLabelConfidence);
            AddMatch(fields, DocumentFieldNames.Amount, InvoiceAmountRegex(), normalizedText,
                NormalizeAmount, ExactLabelConfidence);
        }
        else if (documentType == DocumentType.Contract)
        {
            AddMatch(fields, DocumentFieldNames.Amount, ContractAmountRegex(), normalizedText,
                NormalizeAmount, ContractAmountConfidence);
        }
        else if (documentType == DocumentType.Act)
        {
            AddMatch(fields, DocumentFieldNames.Amount, ActAmountRegex(), normalizedText,
                NormalizeAmount, ActAmountConfidence);
            AddMatch(fields, DocumentFieldNames.RelatedContractNumber, RelatedContractNumberRegex(), normalizedText,
                static value => value, RelatedContractConfidence);
        }

        return Task.FromResult<IReadOnlyCollection<ExtractedDocumentField>>(fields);
    }

    private static Regex GetNumberRegex(DocumentType documentType) => documentType switch
    {
        DocumentType.Invoice => InvoiceNumberRegex(),
        DocumentType.Contract => ContractNumberRegex(),
        DocumentType.Act => ActNumberRegex(),
        _ => throw new ArgumentOutOfRangeException(nameof(documentType), documentType, null)
    };

    private static void AddMatch(
        ICollection<ExtractedDocumentField> fields,
        string name,
        Regex regex,
        string text,
        Func<string, string?> normalize,
        decimal confidence)
    {
        var match = regex.Match(text);
        if (!match.Success)
        {
            return;
        }

        var value = normalize(match.Groups["value"].Value);
        if (value is not null)
        {
            fields.Add(new ExtractedDocumentField(name, value, confidence));
        }
    }

    private static string? NormalizeDate(string value)
    {
        return DateOnly.TryParseExact(
            value,
            SupportedDateFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date)
            ? date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : null;
    }

    private static string? NormalizeAmount(string value)
    {
        var normalized = value.Replace(" ", string.Empty).Replace(',', '.');
        if (!decimal.TryParse(
            normalized,
            NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out var amount))
        {
            return null;
        }

        var separatorIndex = normalized.IndexOf('.');
        if (separatorIndex < 0)
        {
            return amount.ToString("0", CultureInfo.InvariantCulture);
        }

        var decimalPlaces = normalized.Length - separatorIndex - 1;
        return amount.ToString(
            $"0.{new string('0', decimalPlaces)}",
            CultureInfo.InvariantCulture);
    }

    [GeneratedRegex(@"\bсч[её]т(?:\s+на\s+оплату)?\s*(?:№|N)\s*(?<value>[\p{L}\p{Nd}][\p{L}\p{Nd}/-]{0,49})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex InvoiceNumberRegex();

    [GeneratedRegex(@"\bдоговор\s*(?:№|N)\s*(?<value>[\p{L}\p{Nd}][\p{L}\p{Nd}/-]{0,49})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ContractNumberRegex();

    [GeneratedRegex(@"\bакт(?:\s+(?:выполненных\s+работ|оказанных\s+услуг))?\s*(?:№|N)\s*(?<value>[\p{L}\p{Nd}][\p{L}\p{Nd}/-]{0,49})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ActNumberRegex();

    [GeneratedRegex(@"(?:\bот\b|\bдата\s*:?)\s*(?<value>\d{1,2}[./-]\d{1,2}[./-]\d{4})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DateRegex();

    [GeneratedRegex(@"\bИНН(?:\s*/\s*КПП)?\s*:?[ \t]*(?<value>\d{10}|\d{12})(?!\d)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex InnRegex();

    [GeneratedRegex(@"(?:ИНН\s*/\s*КПП\s*:?\s*\d{10}(?!\d)\s*/\s*|\bКПП\s*:?\s*)(?<value>\d{9})(?!\d)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex KppRegex();

    [GeneratedRegex(@"(?:итого\s+к\s+оплате|всего\s+к\s+оплате|сумма\s+к\s+оплате)\s*:?\s*(?<value>(?:\d{1,3}(?: \d{3})+|\d+)(?:[.,]\d{1,2})?)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex InvoiceAmountRegex();

    [GeneratedRegex(@"(?:цена\s+договора|стоимость\s+договора|сумма\s+договора|общая\s+стоимость\s+по\s+договору)\s*:?\s*(?<value>(?:\d{1,3}(?: \d{3})+|\d+)(?:[.,]\d{1,2})?)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ContractAmountRegex();

    [GeneratedRegex(@"(?:стоимость\s+работ|итого|всего|сумма)\s*:?\s*(?<value>(?:\d{1,3}(?: \d{3})+|\d+)(?:[.,]\d{1,2})?)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ActAmountRegex();

    [GeneratedRegex(@"\b(?:к|по)\s+договору\s*(?:№|N)\s*(?<value>[\p{L}\p{Nd}][\p{L}\p{Nd}/-]{0,49})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RelatedContractNumberRegex();
}
