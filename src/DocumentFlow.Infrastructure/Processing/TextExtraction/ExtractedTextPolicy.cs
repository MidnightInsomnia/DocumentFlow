using DocumentFlow.Application.Documents.Processing;

namespace DocumentFlow.Infrastructure.Processing.TextExtraction;

internal static class ExtractedTextPolicy
{
    public const int MinimumLength = 20;

    public static string EnsureUsable(
        string? text,
        TextExtractionFailureReason reason,
        string errorMessage)
    {
        var normalized = text?.Trim() ?? string.Empty;
        if (normalized.Length < MinimumLength)
        {
            throw new TextExtractionException(reason, errorMessage);
        }

        return normalized;
    }
}
