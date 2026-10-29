using System.Text.RegularExpressions;
using DocumentFlow.Application.Abstractions.Processing;
using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Application.Documents.Processing.Classification;

public sealed class RuleBasedDocumentClassifier : IDocumentClassifier
{
    public const decimal MinimumClassificationConfidence = 0.60m;
    public const decimal MinimumWinnerMargin = 0.15m;

    private static readonly IReadOnlyDictionary<DocumentType, Marker[]> Rules =
        new Dictionary<DocumentType, Marker[]>
        {
            [DocumentType.Invoice] =
            [
                new("счет на оплату", 0.60m),
                new("итого к оплате", 0.30m),
                new("банк получателя", 0.20m),
                new("получатель", 0.10m),
                new("поставщик", 0.10m)
            ],
            [DocumentType.Contract] =
            [
                new("предмет договора", 0.50m),
                new("договор", 0.35m),
                new("права и обязанности", 0.20m),
                new("реквизиты сторон", 0.20m),
                new("срок действия", 0.10m)
            ],
            [DocumentType.Act] =
            [
                new("акт выполненных работ", 0.60m),
                new("акт оказанных услуг", 0.60m),
                new("оказанных услуг", 0.40m),
                new("выполненных работ", 0.40m),
                new("акт", 0.20m),
                new("сдал", 0.10m),
                new("принял", 0.10m)
            ]
        };

    public Task<DocumentClassificationResult> ClassifyAsync(
        string text,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(text);

        var normalizedText = Normalize(text);
        var scores = Rules
            .Select(rule => new
            {
                Type = rule.Key,
                Score = Math.Min(
                    1m,
                    rule.Value
                        .Where(marker => ContainsMarker(normalizedText, marker.Text))
                        .Sum(marker => marker.Weight))
            })
            .OrderByDescending(result => result.Score)
            .ToArray();

        var winner = scores[0];
        var runnerUp = scores[1];
        var isConfident = winner.Score >= MinimumClassificationConfidence &&
                          winner.Score - runnerUp.Score >= MinimumWinnerMargin;

        return Task.FromResult(new DocumentClassificationResult(
            isConfident ? winner.Type : DocumentType.Unknown,
            winner.Score));
    }

    private static string Normalize(string text)
    {
        var normalized = text.Trim().ToLowerInvariant().Replace('ё', 'е');
        return Regex.Replace(normalized, @"\s+", " ");
    }

    private static bool ContainsMarker(string text, string marker)
    {
        return Regex.IsMatch(
            text,
            $@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(marker)}(?![\p{{L}}\p{{N}}])",
            RegexOptions.CultureInvariant);
    }

    private sealed record Marker(string Text, decimal Weight);
}
