using System.Text.RegularExpressions;
using DocumentFlow.Application.Abstractions.Processing;
using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Application.Documents.Processing.Classification;

public sealed class RuleBasedDocumentClassifierV2 : IDocumentClassifier
{
    public const decimal MinimumClassificationConfidence = 0.60m;
    public const decimal MinimumWinnerMargin = 0.15m;

    private static readonly IReadOnlyDictionary<DocumentType, Marker[]> Rules =
        new Dictionary<DocumentType, Marker[]>
        {
            [DocumentType.Invoice] =
            [
                new("счет на оплату", .65m), new("счет поставщика", .65m),
                new("счет для оплаты", .65m), new("платежный счет", .65m),
                new("документ к оплате", .65m), new("оплата по счету", .65m),
                new("расчетный документ", .45m), new("итого к оплате", .30m),
                new("к перечислению", .25m), new("банк получателя", .20m),
                new("поставщик", .10m), new("счет", .10m)
            ],
            [DocumentType.Contract] =
            [
                new("договор оказания услуг", .65m), new("договор поставки", .65m),
                new("хозяйственный договор", .65m), new("соглашение о работах", .65m),
                new("соглашение сторон", .65m), new("предмет договора", .55m),
                new("общая стоимость соглашения", .30m), new("регистрационный номер", .20m),
                new("права и обязанности", .20m), new("реквизиты сторон", .20m),
                new("срок действия", .15m), new("контракт", .45m),
                new("соглашение", .35m), new("договор", .15m)
            ],
            [DocumentType.Act] =
            [
                new("акт выполненных работ", .70m), new("акт оказанных услуг", .70m),
                new("акт сдачи работ", .70m), new("приемочный акт", .70m),
                new("акт результатов", .70m), new("документ приемки работ", .65m),
                new("итоговый акт", .70m),
                new("стоимость принятых работ", .30m), new("номер акта", .25m),
                new("работы приняты", .25m), new("оказанных услуг", .40m),
                new("выполненных работ", .40m), new("сдал", .10m),
                new("принял", .10m), new("акт", .10m)
            ]
        };

    public Task<DocumentClassificationResult> ClassifyAsync(string text, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(text);
        var normalized = Regex.Replace(text.Trim().ToLowerInvariant().Replace('ё', 'е'), @"\s+", " ");
        var scores = Rules.Select(rule => new
            {
                Type = rule.Key,
                Score = Math.Min(1m, rule.Value.Where(marker => Contains(normalized, marker.Text)).Sum(marker => marker.Weight))
            })
            .OrderByDescending(result => result.Score)
            .ToArray();
        var winner = scores[0];
        var confident = winner.Score >= MinimumClassificationConfidence &&
                        winner.Score - scores[1].Score >= MinimumWinnerMargin;
        return Task.FromResult(new DocumentClassificationResult(confident ? winner.Type : DocumentType.Unknown, winner.Score));
    }

    private static bool Contains(string text, string marker) => Regex.IsMatch(
        text,
        $@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(marker)}(?![\p{{L}}\p{{N}}])",
        RegexOptions.CultureInvariant);

    private sealed record Marker(string Text, decimal Weight);
}
