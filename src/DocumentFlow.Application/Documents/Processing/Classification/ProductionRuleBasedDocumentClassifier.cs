using System.Text.RegularExpressions;
using DocumentFlow.Application.Abstractions.Processing;
using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Application.Documents.Processing.Classification;

/// <summary>Header-first rules for the running application; research classifiers remain frozen.</summary>
public sealed class ProductionRuleBasedDocumentClassifier : IDocumentClassifier
{
    private static readonly Regex ActHeading = new(
        @"^(?:акт|akt)(?=\s|№|\d|$)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex UnsupportedActHeading = new(
        @"^(?:акт|akt)\s+(?:сверк\p{L}*|инвентаризаци\p{L}*|списани\p{L}*|при[её]ма[\s-]+передачи\p{L}*|расследовани\p{L}*|осмотр\p{L}*)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex SupportedActPurpose = new(
        @"\b(?:выполненных\s+работ|оказанных\s+услуг|сдачи[\s-]+при[её]мки\s+(?:(?:выполненных|оказанных)\s+)?(?:работ|услуг)|об\s+оказании\s+услуг|выполнения\s+работ)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex ContractHeading = new(
        @"^договор(?=\s|№|\d|$)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex InvoiceHeading = new(
        @"^сч[её]т(?:\s+на\s+оплату)?(?=\s|№|\d|$)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex PaymentOrderHeading = new(
        @"^плат[её]жное\s+поручение(?=\s|№|\d|$)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    internal static bool IsSupportedActHeading(string line) =>
        ActHeading.IsMatch(line) &&
        !IsUnsupportedActHeading(line) &&
        SupportedActPurpose.IsMatch(line);

    internal static bool IsUnsupportedActHeading(string line) =>
        UnsupportedActHeading.IsMatch(line);

    public Task<DocumentClassificationResult> ClassifyAsync(
        string text, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(text);

        foreach (var line in text.Split('\n')
                     .Select(value => value.Trim())
                     .Where(value => value.Length > 0)
                     .Take(8))
        {
            // Once the document's basis starts, subsequent contract mentions are body context.
            if (line.StartsWith("Основание", StringComparison.OrdinalIgnoreCase))
                break;
            // A payment order may mention an invoice or contract immediately below
            // its own heading. Those are references, not its document type.
            if (PaymentOrderHeading.IsMatch(line))
                return Task.FromResult(new DocumentClassificationResult(DocumentType.Unknown, 0m));
            if (ActHeading.IsMatch(line))
            {
                // An unsupported or merely generic Act heading must not be rescued by
                // later body mentions of services, invoices or contracts.
                if (!IsSupportedActHeading(line))
                    return Task.FromResult(new DocumentClassificationResult(DocumentType.Unknown, 0m));
                return Task.FromResult(new DocumentClassificationResult(DocumentType.Act, 0.95m));
            }
            if (ContractHeading.IsMatch(line))
                return Task.FromResult(new DocumentClassificationResult(DocumentType.Contract, 0.95m));
            if (InvoiceHeading.IsMatch(line))
                return Task.FromResult(new DocumentClassificationResult(DocumentType.Invoice, 0.95m));
        }

        return Task.FromResult(new DocumentClassificationResult(DocumentType.Unknown, 0m));
    }
}
