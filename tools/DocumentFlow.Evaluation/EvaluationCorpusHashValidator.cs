using System.Security.Cryptography;

namespace DocumentFlow.Evaluation;

public static class EvaluationCorpusHashValidator
{
    public static IReadOnlyCollection<string> FindDuplicateHashes(
        EvaluationManifest manifest,
        EvaluationFileStorage storage) => manifest.Documents
        .Select(item => new { item.Id, Hash = Hash(storage.ResolvePath(item.File)) })
        .GroupBy(item => item.Hash, StringComparer.OrdinalIgnoreCase)
        .Where(group => group.Count() > 1)
        .Select(group => $"Duplicate binary content: {string.Join(", ", group.Select(item => item.Id))}.")
        .ToArray();

    public static IReadOnlyCollection<string> FindCrossCorpusOverlaps(
        EvaluationManifest manifest,
        EvaluationFileStorage storage,
        string referenceFilesDirectory)
    {
        if (!Directory.Exists(referenceFilesDirectory))
            throw new DirectoryNotFoundException($"Reference corpus files directory was not found: {referenceFilesDirectory}");
        var referenceHashes = Directory.EnumerateFiles(referenceFilesDirectory, "*", SearchOption.AllDirectories)
            .ToDictionary(Hash, Path.GetFileName, StringComparer.OrdinalIgnoreCase);
        return manifest.Documents
            .Select(item => new { item.Id, Hash = Hash(storage.ResolvePath(item.File)) })
            .Where(item => referenceHashes.ContainsKey(item.Hash))
            .Select(item => $"Cross-corpus duplicate: {item.Id} matches {referenceHashes[item.Hash]}.")
            .ToArray();
    }

    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
}
