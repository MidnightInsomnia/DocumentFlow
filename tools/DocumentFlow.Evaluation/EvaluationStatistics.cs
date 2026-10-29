namespace DocumentFlow.Evaluation;

public sealed record ConfidenceInterval(double LowerPercent, double UpperPercent);
public sealed record McNemarExactResult(int B, int C, double TwoSidedPValue);

public static class EvaluationStatistics
{
    public static ConfidenceInterval Wilson95(int successes, int total)
    {
        if (total <= 0 || successes < 0 || successes > total)
            throw new ArgumentOutOfRangeException(nameof(total));
        const double z = 1.959963984540054;
        var p = (double)successes / total;
        var denominator = 1 + z * z / total;
        var center = (p + z * z / (2 * total)) / denominator;
        var margin = z * Math.Sqrt(p * (1 - p) / total + z * z / (4d * total * total)) / denominator;
        return new ConfidenceInterval(Math.Max(0, center - margin) * 100, Math.Min(1, center + margin) * 100);
    }

    public static McNemarExactResult McNemarExact(int rulesOnlyExact, int hybridOnlyExact)
    {
        if (rulesOnlyExact < 0 || hybridOnlyExact < 0) throw new ArgumentOutOfRangeException();
        var n = rulesOnlyExact + hybridOnlyExact;
        if (n == 0) return new McNemarExactResult(rulesOnlyExact, hybridOnlyExact, 1);
        var tail = Math.Min(rulesOnlyExact, hybridOnlyExact);
        var probability = 0d;
        for (var k = 0; k <= tail; k++) probability += BinomialCoefficient(n, k) * Math.Pow(0.5, n);
        return new McNemarExactResult(rulesOnlyExact, hybridOnlyExact, Math.Min(1, 2 * probability));
    }

    private static double BinomialCoefficient(int n, int k)
    {
        k = Math.Min(k, n - k);
        var result = 1d;
        for (var i = 1; i <= k; i++) result = result * (n - k + i) / i;
        return result;
    }
}
