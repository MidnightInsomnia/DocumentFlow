using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Evaluation;

public static class EvaluationMetricsCalculator
{
    public static EvaluationSummary Calculate(
        IReadOnlyCollection<EvaluationDocumentResult> allResults,
        DateTime generatedAtUtc)
    {
        var executed = allResults.Where(result => !result.Skipped).ToArray();
        var overall = CalculateGroup(executed);
        var completed = executed.Count(result => result.ActualStatus == DocumentStatus.Completed);
        var needsReview = executed.Count(result => result.ActualStatus == DocumentStatus.NeedsReview);
        var failed = executed.Count(result => result.ActualStatus == DocumentStatus.Failed);
        var sourceKinds = Enum.GetValues<EvaluationSourceKind>().ToDictionary(
            kind => kind,
            kind => CalculateGroup(executed.Where(result => result.SourceKind == kind).ToArray()));
        var difficulties = Enum.GetValues<EvaluationDifficulty>().ToDictionary(
            difficulty => difficulty,
            difficulty => CalculateGroup(executed.Where(result => result.Difficulty == difficulty).ToArray()));
        var fieldMetrics = CalculateFieldMetrics(executed);
        var matrix = Enum.GetValues<DocumentType>().ToDictionary(
            expected => expected,
            expected => (IReadOnlyDictionary<DocumentType, int>)Enum.GetValues<DocumentType>().ToDictionary(
                actual => actual,
                actual => executed.Count(result => result.ExpectedType == expected && result.ActualType == actual)));

        return new EvaluationSummary(
            generatedAtUtc,
            allResults.Count,
            executed.Length,
            allResults.Count - executed.Length,
            overall,
            completed,
            needsReview,
            failed,
            Percent(completed, executed.Length),
            sourceKinds,
            difficulties,
            fieldMetrics,
            matrix,
            allResults);
    }

    public static IReadOnlyCollection<EvaluationFieldComparison> CompareFields(
        IReadOnlyDictionary<string, string> expected,
        IReadOnlyDictionary<string, string> actual)
    {
        var names = expected.Keys.Concat(actual.Keys).Distinct(StringComparer.Ordinal).OrderBy(name => name);
        return names.Select(name =>
        {
            var hasExpected = expected.TryGetValue(name, out var expectedValue);
            var hasActual = actual.TryGetValue(name, out var actualValue);
            var result = hasExpected switch
            {
                true when !hasActual => "Missing",
                true when string.Equals(expectedValue, actualValue, StringComparison.Ordinal) => "Correct",
                true => "WrongValue",
                false => "Unexpected"
            };
            return new EvaluationFieldComparison(name, expectedValue, actualValue, result);
        }).ToArray();
    }

    public static bool IsExactMatch(
        EvaluationManifestDocument expected,
        DocumentType actualType,
        DocumentStatus actualStatus,
        Department actualDepartment,
        IReadOnlyDictionary<string, string> actualFields) =>
        expected.ExpectedType == actualType &&
        expected.ExpectedStatus == actualStatus &&
        expected.ExpectedDepartment == actualDepartment &&
        expected.ExpectedFields.Count == actualFields.Count &&
        expected.ExpectedFields.All(pair => actualFields.TryGetValue(pair.Key, out var value) &&
            string.Equals(pair.Value, value, StringComparison.Ordinal));

    public static EvaluationTimingMetrics CalculateTiming(IEnumerable<long> values)
    {
        var sorted = values.OrderBy(value => value).ToArray();
        if (sorted.Length == 0) return new EvaluationTimingMetrics(0, 0, 0);
        var average = (decimal)sorted.Average();
        var median = sorted.Length % 2 == 1
            ? sorted[sorted.Length / 2]
            : (sorted[sorted.Length / 2 - 1] + sorted[sorted.Length / 2]) / 2m;
        var p95Index = Math.Max(0, (int)Math.Ceiling(0.95m * sorted.Length) - 1);
        return new EvaluationTimingMetrics(
            decimal.Round(average, 2),
            decimal.Round(median, 2),
            sorted[p95Index]);
    }

    private static EvaluationGroupMetrics CalculateGroup(IReadOnlyCollection<EvaluationDocumentResult> results)
    {
        var comparisons = results.SelectMany(result => result.FieldResults).ToArray();
        var correct = comparisons.Count(item => item.Result == "Correct");
        return new EvaluationGroupMetrics(
            results.Count,
            Percent(results.Count(result => result.ClassificationCorrect), results.Count),
            Percent(correct, comparisons.Length),
            Percent(results.Count(result => result.ExactMatch), results.Count),
            Percent(results.Count(result => result.ActualStatus == DocumentStatus.NeedsReview), results.Count),
            Percent(results.Count(result => result.ActualStatus == DocumentStatus.Failed), results.Count),
            CalculateTiming(results.Select(result => result.ProcessingTimeMs)));
    }

    private static IReadOnlyCollection<EvaluationFieldMetrics> CalculateFieldMetrics(
        IEnumerable<EvaluationDocumentResult> results) => results
        .SelectMany(result => result.FieldResults)
        .GroupBy(item => item.Field, StringComparer.Ordinal)
        .OrderBy(group => group.Key)
        .Select(group =>
        {
            var correct = group.Count(item => item.Result == "Correct");
            var missing = group.Count(item => item.Result == "Missing");
            var wrong = group.Count(item => item.Result == "WrongValue");
            var unexpected = group.Count(item => item.Result == "Unexpected");
            return new EvaluationFieldMetrics(
                group.Key,
                correct + missing + wrong,
                correct,
                missing,
                wrong,
                unexpected,
                Percent(correct, correct + missing + wrong + unexpected));
        }).ToArray();

    private static decimal Percent(int numerator, int denominator) => denominator == 0
        ? 0
        : decimal.Round(numerator * 100m / denominator, 2);
}
