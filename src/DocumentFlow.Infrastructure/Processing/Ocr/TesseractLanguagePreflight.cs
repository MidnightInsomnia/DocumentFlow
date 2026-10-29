using DocumentFlow.Application.Documents.Processing;

namespace DocumentFlow.Infrastructure.Processing.Ocr;

public static class TesseractLanguagePreflight
{
    public static string[] ParseAvailableLanguages(string output) =>
        output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(IsLanguageIdentifier).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    public static bool IsLanguageIdentifier(string value)
    {
        if (value.Length is 0 or > 80) return false;
        var parts = value.Split('/');
        return (parts.Length == 1 || (parts.Length == 2 && parts[0] == "script")) &&
            parts.All(part => part.Length > 0 && part.All(character => char.IsAsciiLetterOrDigit(character) || character == '_'));
    }

    public static void EnsureAvailable(string configuredLanguages, IReadOnlyCollection<string> availableLanguages)
    {
        var configured = configuredLanguages.Split(
            '+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (configured.Length == 0)
        {
            throw new TextExtractionException(
                TextExtractionFailureReason.OcrFailed,
                "[ocr_missing_languages] Не настроены языки Tesseract (Tesseract:Languages).");
        }

        var available = availableLanguages.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = configured.Where(language => !available.Contains(language)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (missing.Length > 0)
        {
            throw new TextExtractionException(
                TextExtractionFailureReason.OcrFailed,
                $"[ocr_missing_languages] Недоступны языки Tesseract: {string.Join(", ", missing)}. " +
                "Проверьте .traineddata и Tesseract:TessdataPath; нужны все языки из Tesseract:Languages.");
        }
    }
}
