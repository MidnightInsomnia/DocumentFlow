using System.Text.RegularExpressions;

namespace DocumentFlow.Application.Documents.Processing.FieldExtraction;

// Only the OCR-corrupted label is recovered. The digits are never corrected or inferred.
internal static class InvoiceSupplierInnExtractor
{
    private static readonly Regex StandardInn = new(
        @"\bИНН(?:\s*/\s*КПП)?\s*:?[ \t]*(?<value>\d{10}|\d{12})(?!\d)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex OcrInn = new(
        @"(?<![\p{L}\p{N}])UHH\b\s*(?:/\s*КПП)?\s*[:\-]?\s*(?<value>[0-9]{10}|[0-9]{12})\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex SupplierHeading = new(
        @"^\s*(?:поставщик|продавец)\s*(?::|$)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex NextPartyHeading = new(
        @"^\s*(?:покупатель|заказчик|грузополучатель|плательщик)\s*(?::|$)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex PartyName = new(
        @"^\s*(?:ООО|OOO|ОАО|ЗАО|ПАО|АО|ИП)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex NextSection = new(
        @"^\s*(?:плат[её]жные\s+реквизиты|банковские\s+реквизиты|банк\s*:|итого\s*:|всего\s+к\s+оплате)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static string Extract(string body)
    {
        var lines = body.Split('\n');
        var supplier = Array.FindIndex(lines, line => SupplierHeading.IsMatch(line.TrimEnd('\r')));
        if (supplier < 0)
        {
            // Preserve the pre-existing standard-label behavior where no party headings exist.
            return StandardInn.Match(body).Groups["value"].Value;
        }

        var first = supplier + 1;
        while (first < lines.Length && string.IsNullOrWhiteSpace(lines[first])) first++;
        var parallelPartyHeaders = first < lines.Length && NextPartyHeading.IsMatch(lines[first].TrimEnd('\r'));
        if (parallelPartyHeaders)
        {
            // In two-column invoices OCR often serializes the headings first, then both parties.
            // Without an identifiable first party, do not guess who owns the following INN.
            first++;
            while (first < lines.Length && !PartyName.IsMatch(lines[first].TrimEnd('\r')))
            {
                if (NextPartyHeading.IsMatch(lines[first].TrimEnd('\r')) || NextSection.IsMatch(lines[first].TrimEnd('\r')))
                    return string.Empty;
                first++;
            }
            if (first == lines.Length) return string.Empty;
            if (!lines.Skip(first + 1).TakeWhile(line => !NextPartyHeading.IsMatch(line.TrimEnd('\r')) && !NextSection.IsMatch(line.TrimEnd('\r')))
                    .Any(line => PartyName.IsMatch(line.TrimEnd('\r'))))
                return string.Empty; // One named party after both headings could be the buyer.
        }
        else
        {
            first = supplier; // Allows "Поставщик: ИНН ..." on the heading line.
        }

        var matches = new List<(string Value, bool Recovered)>();
        var firstPartyName = parallelPartyHeaders;
        for (var i = first; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            if (i > first && (NextPartyHeading.IsMatch(line) || NextSection.IsMatch(line))) break;
            if (parallelPartyHeaders && i > first && PartyName.IsMatch(line)) break;
            if (!parallelPartyHeaders && PartyName.IsMatch(line))
            {
                if (firstPartyName) break; // Second named party, even without a repeated buyer heading.
                firstPartyName = true;
            }
            foreach (Match match in StandardInn.Matches(line)) matches.Add((match.Groups["value"].Value, false));
            foreach (Match match in OcrInn.Matches(line)) matches.Add((match.Groups["value"].Value, true));
        }

        // A damaged label is safe to accept only with a valid, unambiguous checksum.
        // The standard label keeps its old extraction behavior for existing invoices.
        if (matches.Count == 0 || matches.Select(m => m.Value).Distinct(StringComparer.Ordinal).Skip(1).Any())
            return string.Empty;
        var candidate = matches[0];
        return !candidate.Recovered || HasValidInnChecksum(candidate.Value) ? candidate.Value : string.Empty;
    }

    private static bool HasValidInnChecksum(string value)
    {
        if (value.Length == 10)
            return CheckDigit(value, [2, 4, 10, 3, 5, 9, 4, 6, 8]) == value[9] - '0';
        if (value.Length == 12)
            return CheckDigit(value, [7, 2, 4, 10, 3, 5, 9, 4, 6, 8]) == value[10] - '0'
                && CheckDigit(value, [3, 7, 2, 4, 10, 3, 5, 9, 4, 6, 8]) == value[11] - '0';
        return false;
    }

    private static int CheckDigit(string value, ReadOnlySpan<int> weights)
    {
        var sum = 0;
        for (var i = 0; i < weights.Length; i++) sum += (value[i] - '0') * weights[i];
        return sum % 11 % 10;
    }
}
