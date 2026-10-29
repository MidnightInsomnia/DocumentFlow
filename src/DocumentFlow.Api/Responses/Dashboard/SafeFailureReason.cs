using System.Text.RegularExpressions;

namespace DocumentFlow.Api.Responses.Dashboard;

// Stored processing errors may contain provider diagnostics; fail closed rather than echoing them.
public static class SafeFailureReason
{
    private const string Fallback = "Processing failed.";
    private const int MaxLength = 120;
    private static readonly Regex Diagnostics = new(
        @"\b(?:at|stacktrace|exception|npgsql|postgres|sql|select|insert|update|delete|drop|from|where|password|secret|token|connection|traceback)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string Format(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) ||
            raw.Any(character => char.IsControl(character) ||
                character is '/' or '\\' or ':' or ';' or '<' or '>' or '{' or '}' or '[' or ']') ||
            Diagnostics.IsMatch(raw) ||
            raw.Any(character => !char.IsLetterOrDigit(character) &&
                character is not (' ' or '-' or ',' or '.' or '(' or ')')))
            return Fallback;

        var clean = raw.Trim();
        return clean.Length <= MaxLength ? clean : clean[..MaxLength].TrimEnd();
    }
}
