using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Evaluation;

public static class EvaluationReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task WriteAsync(
        EvaluationSummary summary,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(outputDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(outputDirectory, "evaluation-summary.json"),
            JsonSerializer.Serialize(summary, JsonOptions), cancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(outputDirectory, "evaluation-documents.csv"),
            CreateDocumentsCsv(summary), cancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(outputDirectory, "evaluation-fields.csv"),
            CreateFieldsCsv(summary), cancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(outputDirectory, "EVALUATION-REPORT.md"),
            CreateMarkdown(summary), cancellationToken);
    }

    private static string CreateDocumentsCsv(EvaluationSummary summary)
    {
        var builder = new StringBuilder("Id,SourceKind,Difficulty,TemplateId,ExpectedType,ActualType,ClassificationCorrect,ExpectedStatus,ActualStatus,ExpectedDepartment,ActualDepartment,FieldsCorrect,FieldsMissing,FieldsWrong,FieldsUnexpected,ExactMatch,ProcessingTimeMs,Result\n");
        foreach (var item in summary.Documents)
        {
            var values = new object?[]
            {
                item.Id, item.SourceKind, item.Difficulty, item.TemplateId, item.ExpectedType, item.ActualType, item.ClassificationCorrect,
                item.ExpectedStatus, item.ActualStatus, item.ExpectedDepartment, item.ActualDepartment,
                item.FieldResults.Count(field => field.Result == "Correct"),
                item.FieldResults.Count(field => field.Result == "Missing"),
                item.FieldResults.Count(field => field.Result == "WrongValue"),
                item.FieldResults.Count(field => field.Result == "Unexpected"),
                item.ExactMatch, item.ProcessingTimeMs, item.Skipped ? $"Skipped: {item.SkipReason}" : item.Error ?? "Executed"
            };
            builder.AppendLine(string.Join(',', values.Select(Csv)));
        }
        return builder.ToString();
    }

    private static string CreateFieldsCsv(EvaluationSummary summary)
    {
        var builder = new StringBuilder("Field,Expected,Correct,Missing,Wrong,Unexpected,AccuracyPercent\n");
        foreach (var item in summary.Fields)
        {
            builder.AppendLine(string.Join(',', new[]
            {
                Csv(item.Field), item.Expected.ToString(), item.Correct.ToString(), item.Missing.ToString(),
                item.Wrong.ToString(), item.Unexpected.ToString(), item.AccuracyPercent.ToString(CultureInfo.InvariantCulture)
            }));
        }
        return builder.ToString();
    }

    private static string CreateMarkdown(EvaluationSummary summary)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# DocumentFlow Evaluation Report\n");
        builder.AppendLine($"Corpus: **{summary.CorpusSize}**  ");
        builder.AppendLine($"Executed: **{summary.Executed}**  ");
        builder.AppendLine($"Skipped: **{summary.Skipped}**\n");
        builder.AppendLine("## Overall\n");
        builder.AppendLine($"- Classification accuracy: {summary.Overall.ClassificationAccuracyPercent}%");
        builder.AppendLine($"- Field accuracy: {summary.Overall.FieldAccuracyPercent}%");
        builder.AppendLine($"- Document exact match: {summary.Overall.ExactMatchPercent}%");
        builder.AppendLine($"- Automatic completion rate: {summary.AutomaticCompletionRate}%");
        builder.AppendLine($"- NeedsReview rate: {summary.Overall.NeedsReviewRate}%");
        builder.AppendLine($"- Failure rate: {summary.Overall.FailureRate}%");
        builder.AppendLine($"- Average / Median / P95: {summary.Overall.Timing.AverageMs} / {summary.Overall.Timing.MedianMs} / {summary.Overall.Timing.P95Ms} ms\n");
        builder.AppendLine("## Classification confusion matrix\n");
        var types = Enum.GetValues<DocumentType>();
        builder.AppendLine("| Expected \\ Actual | " + string.Join(" | ", types) + " |");
        builder.AppendLine("|---|" + string.Join(string.Empty, types.Select(_ => "---|")));
        foreach (var expected in types)
            builder.AppendLine($"| {expected} | {string.Join(" | ", types.Select(actual => summary.ConfusionMatrix[expected][actual]))} |");
        builder.AppendLine("\n## Fields\n");
        builder.AppendLine("| Field | Expected | Correct | Missing | Wrong | Unexpected | Accuracy |\n|---|---:|---:|---:|---:|---:|---:|");
        foreach (var field in summary.Fields)
            builder.AppendLine($"| {field.Field} | {field.Expected} | {field.Correct} | {field.Missing} | {field.Wrong} | {field.Unexpected} | {field.AccuracyPercent}% |");
        builder.AppendLine("\n## By source type\n");
        builder.AppendLine("| Source | Documents | Classification | Fields | Exact | Avg ms | Median ms | P95 ms | NeedsReview | Failed |\n|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|");
        foreach (var (kind, metrics) in summary.SourceKinds)
            builder.AppendLine($"| {kind} | {metrics.DocumentCount} | {metrics.ClassificationAccuracyPercent}% | {metrics.FieldAccuracyPercent}% | {metrics.ExactMatchPercent}% | {metrics.Timing.AverageMs} | {metrics.Timing.MedianMs} | {metrics.Timing.P95Ms} | {metrics.NeedsReviewRate}% | {metrics.FailureRate}% |");
        builder.AppendLine("\n## By difficulty\n");
        builder.AppendLine("| Difficulty | Documents | Classification | Fields | Exact | Avg ms | Median ms | P95 ms | NeedsReview | Failed |\n|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|");
        foreach (var (difficulty, metrics) in summary.Difficulties)
            builder.AppendLine($"| {difficulty} | {metrics.DocumentCount} | {metrics.ClassificationAccuracyPercent}% | {metrics.FieldAccuracyPercent}% | {metrics.ExactMatchPercent}% | {metrics.Timing.AverageMs} | {metrics.Timing.MedianMs} | {metrics.Timing.P95Ms} | {metrics.NeedsReviewRate}% | {metrics.FailureRate}% |");
        builder.AppendLine("\n## Failed / mismatched documents\n");
        var mismatches = summary.Documents.Where(item => !item.Skipped && !item.ExactMatch).ToArray();
        if (mismatches.Length == 0) builder.AppendLine("None.");
        else foreach (var item in mismatches)
        {
            var missing = item.FieldResults.Count(field => field.Result == "Missing");
            var wrong = item.FieldResults.Count(field => field.Result == "WrongValue");
            var unexpected = item.FieldResults.Count(field => field.Result == "Unexpected");
            builder.AppendLine($"- `{item.Id}` ({item.SourceKind}, {item.Difficulty}): {item.ExpectedType} → {item.ActualType}; {item.ExpectedStatus} → {item.ActualStatus}; fields missing/wrong/unexpected: {missing}/{wrong}/{unexpected}.");
        }
        if (summary.Skipped > 0)
        {
            builder.AppendLine("\n## Skipped documents\n");
            foreach (var item in summary.Documents.Where(item => item.Skipped)) builder.AppendLine($"- `{item.Id}`: {item.SkipReason}");
        }
        return builder.ToString();
    }

    private static string Csv(object? value)
    {
        var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        return text.ContainsAny([',', '"', '\n', '\r']) ? $"\"{text.Replace("\"", "\"\"")}\"" : text;
    }
}
