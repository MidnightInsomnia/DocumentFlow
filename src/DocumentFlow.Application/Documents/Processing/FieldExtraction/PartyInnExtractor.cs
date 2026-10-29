using System.Text.RegularExpressions;
using DocumentFlow.Domain.Constants;
using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Application.Documents.Processing.FieldExtraction;

// Role-specific extraction is deliberately stricter than the legacy invoice INN path.
internal static class PartyInnExtractor
{
    private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
    private static readonly Regex ParallelHeading = new(
        @"^(?<first>заказчик|исполнитель|подрядчик|поставщик|продавец|покупатель)(?:\s*\|\s*|\s+)(?<second>заказчик|исполнитель|подрядчик|поставщик|продавец|покупатель)\s*\|?\s*$", Options);
    private static readonly Regex PartyHeading = new(
        @"^(?<label>заказчик|исполнитель|подрядчик|поставщик|продавец|покупатель|плательщик|грузополучатель)(?:\s*:\s*|\s+(?=(?:ООО|OOO|ОАО|ЗАО|ПАО|АО|ИП|НПД)\b)|$)", Options);
    private static readonly Regex PartyName = new(@"^(?:ООО|OOO|ОАО|ЗАО|ПАО|АО|ИП|НПД)\b", Options);
    // A flattened two-column row can contain both names, followed by both
    // labelled INNs on the next row. Require two complete, distinct names.
    private const string Name = @"(?:(?:ООО|OOO|ОАО|ЗАО|ПАО|АО)\s+«[^»\r\n]+»|(?:(?:ИП|НПД)\s+)?[\p{L}-]+\s+[\p{L}-]+\s+[\p{L}-]+)";
    private static readonly Regex ParallelNames = new(@"^(?<first>" + Name + @")\s+(?<second>" + Name + @")$", Options);
    private static readonly Regex EmbeddedSecondParty = new(
        @"\s+\|\s+(?=(?:ООО|OOO|ОАО|ЗАО|ПАО|АО|ИП|НПД)\b)", Options);
    private static readonly Regex Section = new(
        @"^(?:банковские\s+реквизиты|плат[её]жные\s+реквизиты|банк\s*:|итого\b|всего\s+к\s+оплате\b|подписи\s+сторон\b|адреса\s+и\s+реквизиты\b)", Options);
    private static readonly Regex StandardInn = new(
        @"\bИНН(?:\s*/\s*КПП)?\s*:?[ \t]*(?<value>\d{10}|\d{12})(?!\d)", Options);
    private static readonly Regex RecoveredInn = new(
        @"(?<![\p{L}\p{N}])UHH\b\s*(?:/\s*КПП)?\s*[:\-]?\s*(?<value>[0-9]{10}|[0-9]{12})\b", Options);

    public static IReadOnlyCollection<(string Name, string Value)> Extract(DocumentType type, string[] lines)
    {
        if (type is not (DocumentType.Invoice or DocumentType.Act or DocumentType.Contract))
            return [];

        var results = new Dictionary<string, string>(StringComparer.Ordinal);
        var ambiguous = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < lines.Length; i++)
        {
            // A PDF text layer can serialize two column headings before either party's
            // requisites. Attribute values by order only if two distinct named blocks follow.
            var parallel = ParallelHeading.Match(lines[i]);
            if (parallel.Success &&
                Role(type, parallel.Groups["first"].Value) is { } firstRole &&
                Role(type, parallel.Groups["second"].Value) is { } secondRole &&
                firstRole != secondRole)
            {
                var pair = ReadParallel(lines, i + 1);
                if (pair is { } values)
                {
                    Add(results, ambiguous, firstRole, values.First);
                    Add(results, ambiguous, secondRole, values.Second);
                }
                else if (HasNearbyInn(lines, i + 1))
                {
                    ambiguous.Add(firstRole);
                    ambiguous.Add(secondRole);
                }
                continue;
            }
            var heading = PartyHeading.Match(lines[i]);
            if (!heading.Success) continue;
            var role = Role(type, heading.Groups["label"].Value);
            if (role is null) continue;

            // Flattened side-by-side headings on one line do not establish column ownership.
            var inlineHeading = PartyHeading.Match(lines[i][heading.Length..].TrimStart());
            if (inlineHeading.Success)
            {
                ambiguous.Add(role);
                if (Role(type, inlineHeading.Groups["label"].Value) is { } inlineRole)
                    ambiguous.Add(inlineRole);
                continue;
            }

            var next = i + 1;
            if (next < lines.Length && PartyHeading.Match(lines[next]) is { Success: true } adjacent &&
                Role(type, adjacent.Groups["label"].Value) is { } otherRole &&
                otherRole != role && !PartyName.IsMatch(lines[i][heading.Length..]) &&
                !StandardInn.IsMatch(lines[i]) && !RecoveredInn.IsMatch(lines[i]))
            {
                // OCR can emit both column headings before their values. Two named,
                // independently numbered blocks are required to assign either column.
                var pair = ReadParallel(lines, next + 1);
                if (pair is { } values)
                {
                    Add(results, ambiguous, role, values.First);
                    Add(results, ambiguous, otherRole, values.Second);
                }
                else if (HasNearbyInn(lines, next + 1))
                {
                    ambiguous.Add(role);
                    ambiguous.Add(otherRole);
                }
                i = next;
                continue;
            }

            var end = Math.Min(lines.Length, i + 21);
            for (var j = i + 1; j < end; j++)
            {
                if (PartyHeading.IsMatch(lines[j]) || Section.IsMatch(lines[j])) { end = j; break; }
            }
            var value = ReadBlock(lines, i, end);
            if (value is not null) Add(results, ambiguous, role, value);
        }
        return results.Where(pair => !ambiguous.Contains(pair.Key))
            .Select(pair => (pair.Key, pair.Value)).ToArray();
    }

    private static (string First, string Second)? ReadParallel(string[] lines, int start)
    {
        // PDF/OCR may preserve rows rather than columns: two names and then two
        // INNs on one line. Both role and name order must be explicit.
        if (start + 1 < lines.Length && ParallelNames.Match(lines[start]) is { Success: true } names &&
            !string.Equals(names.Groups["first"].Value, names.Groups["second"].Value,
                StringComparison.OrdinalIgnoreCase))
        {
            var pair = ParallelInns(lines[start + 1]);
            if (pair is not null) return pair;
        }

        var blocks = new List<List<string>>();
        var end = Math.Min(lines.Length, start + 20);
        for (var i = start; i < end; i++)
        {
            if (PartyHeading.IsMatch(lines[i]) || Section.IsMatch(lines[i])) break;
            var split = EmbeddedSecondParty.Match(lines[i]);
            if (split.Success && blocks.Count == 1)
            {
                var secondName = lines[i][(split.Index + split.Length)..];
                if (PartyName.IsMatch(secondName))
                {
                    blocks[0].Add(lines[i][..split.Index]);
                    blocks.Add([secondName]);
                    continue;
                }
            }
            if (PartyName.IsMatch(lines[i]))
            {
                if (blocks.Count == 2) return null;
                blocks.Add([]);
            }
            if (blocks.Count == 0)
            {
                if (StandardInn.IsMatch(lines[i]) || RecoveredInn.IsMatch(lines[i])) return null;
                continue;
            }
            blocks[^1].Add(lines[i]);
        }
        if (blocks.Count != 2 || string.Equals(blocks[0][0], blocks[1][0],
                StringComparison.OrdinalIgnoreCase)) return null;
        var first = UniqueInn(blocks[0]);
        var second = UniqueInn(blocks[1]);
        return first is not null && second is not null ? (first, second) : null;
    }

    private static (string First, string Second)? ParallelInns(string line)
    {
        // Mixed standard/recovered labels must retain their original left-to-right order.
        var ordered = StandardInn.Matches(line).Cast<Match>()
            .Select(match => (match.Index, Value: match.Groups["value"].Value, Recovered: false))
            .Concat(RecoveredInn.Matches(line).Cast<Match>()
                .Select(match => (match.Index, Value: match.Groups["value"].Value, Recovered: true)))
            .OrderBy(pair => pair.Index).ToArray();
        return ordered.Length == 2 && ordered[0].Value != ordered[1].Value &&
               ordered.All(pair => !pair.Recovered || HasValidChecksum(pair.Value))
            ? (ordered[0].Value, ordered[1].Value) : null;
    }

    private static bool HasNearbyInn(string[] lines, int start)
    {
        for (var i = start; i < lines.Length && i < start + 20; i++)
        {
            if (PartyHeading.IsMatch(lines[i]) || Section.IsMatch(lines[i])) break;
            if (StandardInn.IsMatch(lines[i]) || RecoveredInn.IsMatch(lines[i])) return true;
        }
        return false;
    }

    private static string? ReadBlock(string[] lines, int start, int end)
    {
        var block = new List<string>();
        var heading = PartyHeading.Match(lines[start]);
        var seenName = heading.Success && PartyName.IsMatch(lines[start][heading.Length..]);
        for (var i = start; i < end; i++)
        {
            if (i > start && PartyName.IsMatch(lines[i]))
            {
                if (seenName) break; // An unheaded second party is not part of this block.
                seenName = true;
            }
            block.Add(lines[i]);
        }
        return UniqueInn(block);
    }

    private static string? UniqueInn(IEnumerable<string> lines)
    {
        var values = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in lines)
        {
            foreach (Match match in StandardInn.Matches(line))
                values.Add(match.Groups["value"].Value);
            foreach (Match match in RecoveredInn.Matches(line))
            {
                var value = match.Groups["value"].Value;
                if (HasValidChecksum(value)) values.Add(value);
            }
        }
        return values.Count == 1 ? values.Single() : null;
    }

    private static void Add(Dictionary<string, string> results, HashSet<string> ambiguous,
        string role, string value)
    {
        if (results.TryGetValue(role, out var previous) && previous != value)
            ambiguous.Add(role);
        else
            results[role] = value;
    }

    private static string? Role(DocumentType type, string label)
    {
        if (type == DocumentType.Invoice)
            return label.ToLowerInvariant() switch
            {
                "поставщик" or "продавец" => DocumentFieldNames.SupplierInn,
                "покупатель" => DocumentFieldNames.BuyerInn,
                _ => null
            };
        return label.ToLowerInvariant() switch
        {
            "заказчик" => DocumentFieldNames.CustomerInn,
            "исполнитель" or "подрядчик" => DocumentFieldNames.ContractorInn,
            _ => null
        };
    }

    // Checksum is required only for a recovered OCR label, not a standard ИНН label.
    private static bool HasValidChecksum(string value) => value.Length switch
    {
        10 => Digit(value, [2, 4, 10, 3, 5, 9, 4, 6, 8]) == value[9] - '0',
        12 => Digit(value, [7, 2, 4, 10, 3, 5, 9, 4, 6, 8]) == value[10] - '0' &&
              Digit(value, [3, 7, 2, 4, 10, 3, 5, 9, 4, 6, 8]) == value[11] - '0',
        _ => false
    };

    private static int Digit(string value, ReadOnlySpan<int> weights)
    {
        var sum = 0;
        for (var i = 0; i < weights.Length; i++) sum += (value[i] - '0') * weights[i];
        return sum % 11 % 10;
    }
}
