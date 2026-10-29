using System.Globalization;
using System.Text.RegularExpressions;
using DocumentFlow.Application.Abstractions.Processing;
using DocumentFlow.Domain.Constants;
using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Application.Documents.Processing.FieldExtraction;

public sealed partial class RuleBasedDocumentFieldExtractorV2 : IDocumentFieldExtractor
{
    private const decimal ExactLabelConfidence = .95m;
    private static readonly string[] DateFormats =
        ["dd.MM.yyyy", "dd/MM/yyyy", "dd-MM-yyyy", "d.M.yyyy", "d/M/yyyy", "d-M-yyyy"];
    private const string NumberValue = @"[\p{L}\p{Nd}][\p{L}\p{Nd}/-]{0,49}";
    private const string AmountValue = @"(?:\d{1,3}(?:[ \u00A0]\d{3})+|\d+)(?:[.,]\d{1,2})?";

    public Task<IReadOnlyCollection<ExtractedDocumentField>> ExtractAsync(
        DocumentType documentType, string text, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(text);
        cancellationToken.ThrowIfCancellationRequested();
        if (documentType == DocumentType.Unknown)
            return Task.FromResult<IReadOnlyCollection<ExtractedDocumentField>>([]);

        var normalized = text.Replace('\u00A0', ' ').Replace("\r\n", "\n").Replace('\r', '\n');
        var fields = new List<ExtractedDocumentField>();
        if (documentType == DocumentType.Invoice)
        {
            AddAfterLabel(fields, DocumentFieldNames.Number, normalized, DocumentFieldLabelLexicon.InvoiceNumber,
                NumberValue, Identity, ExactLabelConfidence);
        }
        else if (documentType == DocumentType.Contract)
        {
            AddAfterLabel(fields, DocumentFieldNames.Number, normalized, ["регистрационный номер"],
                NumberValue, Identity, ExactLabelConfidence);
            AddRegex(fields, DocumentFieldNames.Number, normalized, ContractNumberRegex(), Identity, ExactLabelConfidence);
        }
        else if (documentType == DocumentType.Act)
        {
            AddAfterLabel(fields, DocumentFieldNames.Number, normalized, ["номер акта", "номер документа"],
                NumberValue, Identity, ExactLabelConfidence);
            AddRegex(fields, DocumentFieldNames.Number, normalized, ActNumberRegex(), Identity, ExactLabelConfidence);
        }
        AddAfterLabel(fields, DocumentFieldNames.Date, normalized, DocumentFieldLabelLexicon.Date,
            @"\d{1,2}[./-]\d{1,2}[./-]\d{4}", NormalizeDate, .90m);
        AddRegex(fields, DocumentFieldNames.Date, normalized, DateAfterOtRegex(), NormalizeDate, .90m);

        if (documentType == DocumentType.Invoice)
        {
            AddRegex(fields, DocumentFieldNames.Inn, normalized, InnRegex(), Identity, ExactLabelConfidence);
            AddRegex(fields, DocumentFieldNames.Kpp, normalized, KppRegex(), Identity, ExactLabelConfidence);
            AddAfterLabel(fields, DocumentFieldNames.Amount, normalized, DocumentFieldLabelLexicon.InvoiceAmount,
                AmountValue, NormalizeAmount, ExactLabelConfidence);
        }
        else if (documentType == DocumentType.Contract)
        {
            AddAfterLabel(fields, DocumentFieldNames.Amount, normalized, DocumentFieldLabelLexicon.ContractAmount,
                AmountValue, NormalizeAmount, .92m);
        }
        else if (documentType == DocumentType.Act)
        {
            AddAfterLabel(fields, DocumentFieldNames.Amount, normalized, DocumentFieldLabelLexicon.ActAmount,
                AmountValue, NormalizeAmount, .88m);
            AddAfterLabel(fields, DocumentFieldNames.RelatedContractNumber, normalized,
                DocumentFieldLabelLexicon.RelatedContractNumber, NumberValue, Identity, .93m);
        }

        return Task.FromResult<IReadOnlyCollection<ExtractedDocumentField>>(fields);
    }

    private static void AddAfterLabel(ICollection<ExtractedDocumentField> fields, string name, string text,
        IEnumerable<string> labels, string valuePattern, Func<string, string?> normalize, decimal confidence)
    {
        if (fields.Any(field => field.Name == name)) return;
        var labelPattern = string.Join("|", labels.OrderByDescending(label => label.Length).Select(LabelPattern));
        var regex = new Regex(
            $@"(?<![\p{{L}}\p{{N}}])(?:{labelPattern})(?![\p{{L}}\p{{N}}])\s*(?:(?:№|N)\s*)?(?::|—|-)?\s*(?<value>{valuePattern})",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        AddRegex(fields, name, text, regex, normalize, confidence);
    }

    private static string LabelPattern(string label) => string.Join(@"\s+", label.Split(' ').Select(Regex.Escape));

    private static void AddRegex(ICollection<ExtractedDocumentField> fields, string name, string text, Regex regex,
        Func<string, string?> normalize, decimal confidence)
    {
        if (fields.Any(field => field.Name == name)) return;
        var match = regex.Match(text);
        if (!match.Success) return;
        var value = normalize(match.Groups["value"].Value);
        if (value is not null) fields.Add(new ExtractedDocumentField(name, value, confidence));
    }

    private static string? Identity(string value) => value;
    private static string? NormalizeDate(string value) => DateOnly.TryParseExact(value, DateFormats,
        CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
        ? date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : null;
    private static string? NormalizeAmount(string value)
    {
        var normalized = value.Replace(" ", "").Replace('\u00A0', ' ').Replace(',', '.');
        if (!decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount)) return null;
        var separator = normalized.IndexOf('.');
        return separator < 0 ? amount.ToString("0", CultureInfo.InvariantCulture) :
            amount.ToString($"0.{new string('0', normalized.Length - separator - 1)}", CultureInfo.InvariantCulture);
    }

    [GeneratedRegex(@"\bот\s+(?<value>\d{1,2}[./-]\d{1,2}[./-]\d{4})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DateAfterOtRegex();
    [GeneratedRegex(@"\b(?:договор|контракт)\s*(?:№|N)\s*(?<value>[\p{L}\p{Nd}][\p{L}\p{Nd}/-]{0,49})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ContractNumberRegex();
    [GeneratedRegex(@"\bакт(?:\s+(?:выполненных\s+работ|оказанных\s+услуг))?\s*(?:№|N)\s*(?<value>[\p{L}\p{Nd}][\p{L}\p{Nd}/-]{0,49})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ActNumberRegex();
    [GeneratedRegex(@"\bИНН(?:\s*/\s*КПП)?\s*:?[ \t]*(?<value>\d{10}|\d{12})(?!\d)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex InnRegex();
    [GeneratedRegex(@"(?:ИНН\s*/\s*КПП\s*:?\s*\d{10}(?!\d)\s*/\s*|\bКПП\s*:?\s*)(?<value>\d{9})(?!\d)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex KppRegex();
}
