using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Evaluation;

public enum EvaluationSourceKind { TextPdf, ScannedPdf, Image }
public enum EvaluationDifficulty { Easy, Medium, Hard }

public sealed class EvaluationManifest
{
    public List<EvaluationManifestDocument> Documents { get; init; } = [];
}

public sealed class EvaluationManifestDocument
{
    public string Id { get; init; } = "";
    public string File { get; init; } = "";
    public EvaluationSourceKind SourceKind { get; init; }
    public string ContentType { get; init; } = "";
    public EvaluationDifficulty Difficulty { get; init; }
    public string TemplateId { get; init; } = "";
    public DocumentType ExpectedType { get; init; }
    public DocumentStatus ExpectedStatus { get; init; }
    public Department ExpectedDepartment { get; init; }
    public Dictionary<string, string> ExpectedFields { get; init; } = [];
}

public sealed record EvaluationFieldComparison(
    string Field,
    string? Expected,
    string? Actual,
    string Result);

public sealed record EvaluationDocumentResult(
    string Id,
    EvaluationSourceKind SourceKind,
    EvaluationDifficulty Difficulty,
    string TemplateId,
    DocumentType ExpectedType,
    DocumentType ActualType,
    DocumentStatus ExpectedStatus,
    DocumentStatus ActualStatus,
    Department ExpectedDepartment,
    Department ActualDepartment,
    IReadOnlyDictionary<string, string> ExpectedFields,
    IReadOnlyDictionary<string, string> ActualFields,
    bool ClassificationCorrect,
    IReadOnlyCollection<EvaluationFieldComparison> FieldResults,
    bool ExactMatch,
    long ProcessingTimeMs,
    string? Error,
    bool Skipped = false,
    string? SkipReason = null);

public sealed record EvaluationFieldMetrics(
    string Field,
    int Expected,
    int Correct,
    int Missing,
    int Wrong,
    int Unexpected,
    decimal AccuracyPercent);

public sealed record EvaluationTimingMetrics(decimal AverageMs, decimal MedianMs, decimal P95Ms);

public sealed record EvaluationGroupMetrics(
    int DocumentCount,
    decimal ClassificationAccuracyPercent,
    decimal FieldAccuracyPercent,
    decimal ExactMatchPercent,
    decimal NeedsReviewRate,
    decimal FailureRate,
    EvaluationTimingMetrics Timing);

public sealed record EvaluationSummary(
    DateTime GeneratedAtUtc,
    int CorpusSize,
    int Executed,
    int Skipped,
    EvaluationGroupMetrics Overall,
    int CompletedCount,
    int NeedsReviewCount,
    int FailedCount,
    decimal AutomaticCompletionRate,
    IReadOnlyDictionary<EvaluationSourceKind, EvaluationGroupMetrics> SourceKinds,
    IReadOnlyDictionary<EvaluationDifficulty, EvaluationGroupMetrics> Difficulties,
    IReadOnlyCollection<EvaluationFieldMetrics> Fields,
    IReadOnlyDictionary<DocumentType, IReadOnlyDictionary<DocumentType, int>> ConfusionMatrix,
    IReadOnlyCollection<EvaluationDocumentResult> Documents);
