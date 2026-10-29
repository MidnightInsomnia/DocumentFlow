namespace DocumentFlow.Evaluation;

public static class EvaluationCorpusCrossValidator
{
    public static IReadOnlyCollection<string> FindIdAndTemplateOverlaps(
        EvaluationManifest candidate,
        params EvaluationManifest[] references)
    {
        var referenceIds = references.SelectMany(item => item.Documents)
            .Select(item => item.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var referenceTemplates = references.SelectMany(item => item.Documents)
            .Select(item => item.TemplateId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return candidate.Documents.SelectMany(item =>
        {
            var errors = new List<string>();
            if (referenceIds.Contains(item.Id)) errors.Add($"Cross-corpus document id overlap: {item.Id}.");
            if (referenceTemplates.Contains(item.TemplateId)) errors.Add($"Cross-corpus template id overlap: {item.TemplateId}.");
            return errors;
        }).ToArray();
    }
}
