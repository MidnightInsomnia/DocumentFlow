using DocumentFlow.Application.Abstractions.Files;

namespace DocumentFlow.Evaluation;

public sealed class EvaluationFileStorage : IFileStorage
{
    private readonly string _corpusRoot;
    private readonly string _corpusRootPrefix;

    public EvaluationFileStorage(string corpusRoot)
    {
        _corpusRoot = Path.GetFullPath(corpusRoot);
        _corpusRootPrefix = _corpusRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
    }

    public Task<string> SaveAsync(Stream content, string originalFileName, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Evaluation corpus storage is read-only.");

    public Task DeleteAsync(string storedFileName, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Evaluation corpus storage is read-only.");

    public Task<Stream> OpenReadAsync(string storedFileName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolvePath(storedFileName);
        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        return Task.FromResult(stream);
    }

    public string ResolvePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathFullyQualified(relativePath))
        {
            throw new InvalidOperationException("Corpus file path must be a non-empty relative path.");
        }

        var resolved = Path.GetFullPath(Path.Combine(_corpusRoot, relativePath));
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (!resolved.StartsWith(_corpusRootPrefix, comparison))
        {
            throw new InvalidOperationException($"Corpus path escapes the corpus directory: '{relativePath}'.");
        }

        return resolved;
    }
}
