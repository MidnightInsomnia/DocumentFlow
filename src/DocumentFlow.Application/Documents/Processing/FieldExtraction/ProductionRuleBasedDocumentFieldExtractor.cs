using System.Globalization;
using System.Text.RegularExpressions;
using DocumentFlow.Application.Abstractions.Processing;
using DocumentFlow.Application.Documents.Processing.Classification;
using DocumentFlow.Domain.Constants;
using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Application.Documents.Processing.FieldExtraction;

/// <summary>Production-only, type-aware field extraction; research rules are unchanged.</summary>
public sealed class ProductionRuleBasedDocumentFieldExtractor : IDocumentFieldExtractor
{
    private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
    private const string Money = @"(?<value>(?:\d{1,3}(?: \d{3})+|\d+)(?:[.,]\d{1,2})?)";
    private static readonly Regex ActTitle = new(@"^(?:акт|akt)(?:\s+(?:выполненных\s+работ|оказанных\s+услуг))?(?=\s|№|\d|$)", Options);
    private static readonly Regex ContractTitle = new(@"^договор(?=\s|№|\d|$)", Options);
    private static readonly Regex InvoiceTitle = new(@"^сч[её]т(?:\s+на\s+оплату)?(?=\s|№|\d|$)", Options);
    private static readonly Regex ActNumberMarker = new(@"(?<![\p{L}\p{N}])(?:№|N°|Ne|N)(?=\s|[\p{L}\p{N}]|$)", Options);
    private static readonly Regex BareActNumber = new(@"^акт\s+(?<value>\d{1,15})(?![\p{L}\p{N}./-])(?=\s|$)", Options);
    private static readonly Regex NonActNumberContext = new(@"\b(?:договор|приложени[еяю]|заявк[аиу]|сч[её]т|спецификаци[яию]|основани[еяю])\b", Options);
    private static readonly Regex ContractNumberMarker = new(@"(?<![\p{L}\p{N}])(?:№|®|N°|Ne|N)(?=\s|[\p{L}\p{N}]|$)", Options);
    private static readonly Regex ContractNumberToken = new(@"^(?<value>(?=[\p{L}\p{N}./-]*\d)[\p{L}\p{N}](?:[\p{L}\p{N}./-]{0,48}[\p{L}\p{N}])?)(?![\p{L}\p{N}./-])", Options);
    private static readonly Regex ContractNumberRemainder = new(@"^(?:от\b|[.,;]?$)", Options);
    private static readonly Regex NonContractNumberContext = new(@"\b(?:приложени[еяю]|заявк[аиу]|сч[её]т|спецификаци[яию]|основани[еяю])\b", Options);
    private static readonly Regex InvoiceNumber = new(@"^сч[её]т(?:\s+(?:на|ha)\s+оплату)?\s*(?:(?:№|Ne|N°|N)\s*)?(?<value>(?=[\p{L}\p{N}/-]*\d)[\p{L}\p{N}][\p{L}\p{N}/-]{0,49})(?![\p{L}\p{N}/-])", Options);
    private static readonly Regex Date = new(
        @"(?<![\p{L}\d])(?<day>\d{1,2})[./-](?<month>\d{1,2})[./-](?<year>\d{4})(?!\d)|(?<![\p{L}\d])(?:[«""“]\s*)?(?<day>\d{1,2})(?:\s*[»""”])?\s+(?<month>январ[ья]|феврал[ья]|март[а]?|апрел[ья]|ма[йя]|июн[ья]|июл[ья]|август[а]?|сентябр[ья]|октябр[ья]|ноябр[ья]|декабр[ья])\s+(?<year>\d{4})(?!\d)", Options);
    // Tesseract can render "мая" as Latin "maa". Recover only this observed
    // token in an act or invoice heading date, never in an unrelated body date.
    private static readonly Regex HeadingOcrMayDate = new(
        @"^\s*(?:[«""“]\s*)?(?<day>\d{1,2})(?:\s*[»""”])?\s+maa\s+(?<year>\d{4})(?!\d)", Options);
    private static readonly Regex Kpp = new(@"(?:ИНН\s*/\s*КПП\s*:?\s*\d{10}(?!\d)\s*/\s*|\bКПП\s*:?\s*)(?<value>\d{9})(?!\d)", Options);
    private static readonly Regex InvoiceTotal = new(@"\b(?:итого|всего|сумма)\s+к\s+оплате\s*[:|]?\s*" + Money, Options);
    private static readonly Regex ContractTotal = new(@"\b(?:общая\s+стоимость\s+услуг|(?:цена|стоимость|сумма)\s+договора|общая\s+(?:цена|стоимость)\s+(?:договора|по\s+договору)|стоимость\s+работ\s+по\s+настоящему\s+договору|максимальная\s+цена\s+договора)\s*(?:составляет\s*|равна\s*|[:—–-]\s*)?" + Money + @"(?=\s|[.,;)]|$)", Options);
    private static readonly Regex ContractRemuneration = new(@"\bвознаграждение\s+исполнителя\s+составляет\s*" + Money + @"(?=\s|[.,;)]|$)", Options);
    private static readonly Regex ContractPriceSection = new(@"^(?:\d+\.\s*)?(?:стоимость\s+и\s+порядок\s+расч[её]тов|цена\s+договора|стоимость\s+услуг|стоимость\s+работ)\b", Options);
    private static readonly Regex ContractSection = new(@"^\d+\.\s+\p{L}", Options);
    private static readonly Regex ContractAppendix = new(@"^(?:приложение|спецификация)\s*(?:№|N|\d)", Options);
    private static readonly Regex PeriodicRemuneration = new(@"\b(?:ежемесячн\p{L}*|этап\p{L}*|аванс\p{L}*)\b", Options);
    private static readonly Regex ActTotal = new(@"\bитого\s*[:|]?\s*[|]?\s*" + Money + @"(?![\p{L}\p{N}])", Options);
    private static readonly Regex ActServicesTotal = new(@"\bвсего[^\r\n]{0,120}?\bна\s+сумму\s*[:|]?\s*" + Money, Options);
    private static readonly Regex ActWorkTotal = new(@"\bстоимость\s+работ\s*[:|]?\s*[|]?\s*" + Money, Options);
    private static readonly Regex ActOtherTotal = new(@"\bвсего\s*[:|]\s*[|]?\s*" + Money, Options);
    private static readonly Regex ContractMention = new(@"\bдоговор(?:у|а)?\b(?<tail>[^\r\n]{0,140})", Options);
    private static readonly Regex ContractToken = new(@"(?<![\p{L}\p{N}/-])(?<value>(?=[\p{L}\p{N}/-]*\d)[\p{L}\p{N}][\p{L}\p{N}/-]{0,49})(?![\p{L}\p{N}/-])", Options);
    private static readonly Regex DateLabel = new(@"^дата\s*:\s*", Options);
    private static readonly Regex DateFrom = new(@"\b(?:от|ot)\b\s*", Options);
    private static readonly Regex NonDocumentDateContext = new(@"\b(?:основани[еяю]|срок(?:и|а)?|оплатить|оплата)\b", Options);
    private static readonly Dictionary<string, int> Months = new(StringComparer.OrdinalIgnoreCase)
    {
        ["январь"] = 1, ["января"] = 1, ["февраль"] = 2, ["февраля"] = 2,
        ["март"] = 3, ["марта"] = 3, ["апрель"] = 4, ["апреля"] = 4,
        ["май"] = 5, ["мая"] = 5, ["июнь"] = 6, ["июня"] = 6,
        ["июль"] = 7, ["июля"] = 7, ["август"] = 8, ["августа"] = 8,
        ["сентябрь"] = 9, ["сентября"] = 9, ["октябрь"] = 10, ["октября"] = 10,
        ["ноябрь"] = 11, ["ноября"] = 11, ["декабрь"] = 12, ["декабря"] = 12
    };

    public Task<IReadOnlyCollection<ExtractedDocumentField>> ExtractAsync(
        DocumentType documentType, string text, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(text);
        if (documentType == DocumentType.Unknown)
            return Task.FromResult<IReadOnlyCollection<ExtractedDocumentField>>([]);

        var lines = text.Replace('\u00A0', ' ').Replace("\r\n", "\n").Replace('\r', '\n')
            .Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0).ToArray();
        var fields = new List<ExtractedDocumentField>();
        var titlePattern = documentType switch
        {
            DocumentType.Act => ActTitle,
            DocumentType.Contract => ContractTitle,
            DocumentType.Invoice => InvoiceTitle,
            _ => null
        };
        var titleIndex = Array.FindIndex(lines, 0, Math.Min(lines.Length, 8),
            line => titlePattern?.IsMatch(line) == true &&
                (documentType != DocumentType.Act ||
                 (line.StartsWith("AKT", StringComparison.OrdinalIgnoreCase)
                     ? ProductionRuleBasedDocumentClassifier.IsSupportedActHeading(line)
                     : !ProductionRuleBasedDocumentClassifier.IsUnsupportedActHeading(line))));
        if (titleIndex >= 0)
        {
            var title = lines[titleIndex];
            Add(fields, DocumentFieldNames.Number,
                documentType switch
                {
                    DocumentType.Contract => FindContractNumber(lines, titleIndex),
                    DocumentType.Act => FindActNumber(lines, titleIndex),
                    _ => InvoiceNumber.Match(title).Groups["value"].Value
                }, 0.95m);
            Add(fields, DocumentFieldNames.Date, FindDocumentDate(documentType, lines, titleIndex), 0.90m);
        }

        var body = string.Join("\n", lines);
        if (documentType == DocumentType.Act)
        {
            var total = new[] { ActServicesTotal, ActOtherTotal, ActWorkTotal, ActTotal }
                .Select(pattern => pattern.Match(body).Groups["value"].Value)
                .FirstOrDefault(value => value.Length > 0);
            Add(fields, DocumentFieldNames.Amount, NormalizeAmount(total), 0.88m);
            Add(fields, DocumentFieldNames.RelatedContractNumber, FindRelatedContract(lines), 0.93m);
        }
        else if (documentType == DocumentType.Contract)
        {
            Add(fields, DocumentFieldNames.Amount, FindContractAmount(lines), 0.92m);
        }
        else if (documentType == DocumentType.Invoice)
        {
            var parties = PartyInnExtractor.Extract(documentType, lines);
            Add(fields, DocumentFieldNames.Inn,
                parties.FirstOrDefault(party => party.Name == DocumentFieldNames.SupplierInn).Value
                ?? InvoiceSupplierInnExtractor.Extract(body), 0.95m);
            Add(fields, DocumentFieldNames.Kpp, Kpp.Match(body).Groups["value"].Value, 0.95m);
            Add(fields, DocumentFieldNames.Amount,
                NormalizeAmount(InvoiceTotal.Match(body).Groups["value"].Value), 0.95m);
        }
        foreach (var party in PartyInnExtractor.Extract(documentType, lines))
            Add(fields, party.Name, party.Value, 0.95m);
        return Task.FromResult<IReadOnlyCollection<ExtractedDocumentField>>(fields);
    }

    private static string? FindActNumber(string[] lines, int titleIndex)
    {
        for (var index = titleIndex; index < lines.Length && index <= titleIndex + 3; index++)
        {
            var line = lines[index];
            if (index > titleIndex &&
                ((DateFrom.Match(line) is { Success: true, Index: 0 } && Date.IsMatch(line)) ||
                 Date.Match(line) is { Success: true, Index: 0 }))
                continue;

            var marker = ActNumberMarker.Match(line);
            if (index == titleIndex && !marker.Success)
            {
                var bare = BareActNumber.Match(line);
                if (bare.Success)
                    return bare.Groups["value"].Value;
            }
            if (!marker.Success || (index > titleIndex && marker.Index != 0))
            {
                if (index == titleIndex)
                    continue;
                return null;
            }
            if (index == titleIndex &&
                (marker.Index > 120 || NonActNumberContext.IsMatch(line[..marker.Index])))
                return null;

            var tail = line[(marker.Index + marker.Length)..].TrimStart();
            var token = ContractNumberToken.Match(tail);
            if (!token.Success || !ContractNumberRemainder.IsMatch(tail[token.Length..].TrimStart()))
                return null;
            var value = token.Groups["value"].Value;
            return value.Length >= 16 && value.All(char.IsAsciiDigit) ? null : value;
        }
        return null;
    }

    private static string? FindContractAmount(string[] lines)
    {
        var inPriceSection = false;
        foreach (var line in lines)
        {
            if (ContractAppendix.IsMatch(line))
                break;
            if (ContractPriceSection.IsMatch(line))
                inPriceSection = true;
            else if (ContractSection.IsMatch(line))
                inPriceSection = false;

            var total = ContractTotal.Match(line);
            if (total.Success)
                return NormalizeAmount(total.Groups["value"].Value);

            if (!inPriceSection)
                continue;
            var remuneration = ContractRemuneration.Match(line);
            if (remuneration.Success && !PeriodicRemuneration.IsMatch(line[..remuneration.Index]))
                return NormalizeAmount(remuneration.Groups["value"].Value);
        }
        return null;
    }

    private static string? FindContractNumber(string[] lines, int titleIndex)
    {
        // A number marker must belong to the heading or its nearest lines, never to an appendix.
        for (var index = titleIndex; index < lines.Length && index <= titleIndex + 3; index++)
        {
            var line = lines[index];
            if (index > titleIndex &&
                ((DateFrom.Match(line) is { Success: true, Index: 0 } && Date.IsMatch(line)) ||
                 Date.Match(line) is { Success: true, Index: 0 }))
                continue;

            var marker = ContractNumberMarker.Match(line);
            if (!marker.Success || (index > titleIndex && marker.Index != 0))
            {
                if (index == titleIndex)
                    continue;
                return null;
            }
            if (index == titleIndex &&
                (marker.Index > 120 || NonContractNumberContext.IsMatch(line[..marker.Index])))
                return null;

            var tail = line[(marker.Index + marker.Length)..].TrimStart();
            var token = ContractNumberToken.Match(tail);
            if (!token.Success || !ContractNumberRemainder.IsMatch(tail[token.Length..].TrimStart()))
                return null;
            var value = token.Groups["value"].Value;
            return value.Length >= 16 && value.All(char.IsAsciiDigit) ? null : value;
        }
        return null;
    }

    private static string? FindDocumentDate(DocumentType documentType, string[] lines, int titleIndex)
    {
        // Only the heading and up to three following nonempty lines may identify this document's date.
        // Stop at the first unrelated line instead of accepting basis-contract or payment dates.
        var title = lines[titleIndex];
        var from = DateFrom.Match(title);
        if (from.Success)
        {
            var prefix = title[..from.Index];
            if (NonDocumentDateContext.IsMatch(prefix) ||
                (!ContractTitle.IsMatch(title) && ContractMention.IsMatch(prefix)))
                return null;
            var tail = title[(from.Index + from.Length)..];
            if (tail.Length > 0)
                return TryParseRussianDate(tail, documentType is DocumentType.Act or DocumentType.Invoice);
        }

        var awaitingDate = from.Success;
        for (var index = titleIndex + 1; index < lines.Length && index <= titleIndex + 3; index++)
        {
            var line = lines[index];
            // An Act may place its own numbered heading line before the date.
            if (index == titleIndex + 1 && ActTitle.IsMatch(title) &&
                !ActNumberMarker.IsMatch(title) &&
                ActNumberMarker.Match(line) is { Success: true, Index: 0 } &&
                FindActNumber(lines, titleIndex) is not null)
                continue;
            // A Contract may place its own numbered heading line before the date.
            if (index == titleIndex + 1 && ContractTitle.IsMatch(title) &&
                !ContractNumberMarker.IsMatch(title) &&
                ContractNumberMarker.Match(line) is { Success: true, Index: 0 } &&
                FindContractNumber(lines, titleIndex) is not null)
                continue;
            var nextFrom = DateFrom.Match(line);
            if (nextFrom.Success && nextFrom.Index == 0)
            {
                var tail = line[(nextFrom.Index + nextFrom.Length)..];
                if (tail.Length > 0)
                    return TryParseRussianDate(tail, documentType is DocumentType.Act or DocumentType.Invoice);
                awaitingDate = true;
                continue;
            }

            var label = DateLabel.Match(line);
            if (label.Success)
                return TryParseRussianDate(line[label.Length..], documentType is DocumentType.Act or DocumentType.Invoice);

            var date = Date.Match(line);
            if (date.Success && date.Index == 0 && (awaitingDate || index == titleIndex + 1))
                return TryParseRussianDate(line, documentType is DocumentType.Act or DocumentType.Invoice);

            return null;
        }
        return null;
    }

    private static string? TryParseRussianDate(string candidate, bool allowHeadingOcrMay)
    {
        Match? recovered = allowHeadingOcrMay ? HeadingOcrMayDate.Match(candidate) : null;
        var recoveredMay = recovered?.Success == true;
        var match = recoveredMay ? recovered! : Date.Match(candidate);
        if (!match.Success || !int.TryParse(match.Groups["day"].Value, out var day) ||
            !int.TryParse(match.Groups["year"].Value, out var year) || year is < 1 or > 9999)
            return null;
        var monthText = match.Groups["month"].Value;
        var month = recoveredMay ? 5 : int.TryParse(monthText, out var numericMonth)
            ? numericMonth : Months.GetValueOrDefault(monthText);
        return month >= 1 && month <= 12 && day >= 1 && day <= DateTime.DaysInMonth(year, month)
            ? new DateOnly(year, month, day).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : null;
    }

    private static string? FindRelatedContract(IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            var match = ContractMention.Match(line);
            if (!match.Success)
                continue;
            var tail = Regex.Split(match.Groups["tail"].Value, @"\s+от\s+(?=\d)", Options)[0];
            var token = ContractToken.Match(tail);
            if (token.Success)
                return token.Groups["value"].Value;
        }
        return null;
    }

    private static string? NormalizeAmount(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;
        var normalized = input.Replace(" ", "").Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out var amount)
            ? amount.ToString(normalized.Contains('.') ? "0.00" : "0", CultureInfo.InvariantCulture)
            : null;
    }

    private static void Add(ICollection<ExtractedDocumentField> fields,
        string name, string? value, decimal confidence)
    {
        if (!string.IsNullOrWhiteSpace(value))
            fields.Add(new ExtractedDocumentField(name, value, confidence));
    }
}
