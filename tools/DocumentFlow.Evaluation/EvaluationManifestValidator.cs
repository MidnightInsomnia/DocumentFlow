using DocumentFlow.Application.Documents.Processing;
using DocumentFlow.Domain.Constants;
using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Evaluation;

public static class EvaluationManifestValidator
{
    private static readonly HashSet<string> SupportedFields =
    [
        DocumentFieldNames.Number, DocumentFieldNames.Date, DocumentFieldNames.Inn,
        DocumentFieldNames.Kpp, DocumentFieldNames.Amount, DocumentFieldNames.RelatedContractNumber
    ];

    private static readonly HashSet<string> SupportedContentTypes =
    ["application/pdf", "image/jpeg", "image/png"];

    public static IReadOnlyCollection<string> Validate(
        EvaluationManifest manifest,
        EvaluationFileStorage storage)
    {
        var errors = new List<string>();
        foreach (var duplicate in manifest.Documents.GroupBy(item => item.Id, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1))
        {
            errors.Add($"Duplicate document id '{duplicate.Key}'.");
        }

        foreach (var item in manifest.Documents)
        {
            if (string.IsNullOrWhiteSpace(item.Id)) errors.Add("Document id is required.");
            if (string.IsNullOrWhiteSpace(item.TemplateId)) errors.Add($"{item.Id}: templateId is required.");
            else if (item.TemplateId.Length > 100) errors.Add($"{item.Id}: templateId must not exceed 100 characters.");
            if (!Enum.IsDefined(item.Difficulty)) errors.Add($"{item.Id}: invalid difficulty.");
            string? path = null;
            try { path = storage.ResolvePath(item.File); }
            catch (Exception exception) { errors.Add($"{item.Id}: {exception.Message}"); }
            if (path is not null && !File.Exists(path)) errors.Add($"{item.Id}: file '{item.File}' does not exist.");
            if (!SupportedContentTypes.Contains(item.ContentType)) errors.Add($"{item.Id}: unsupported content type '{item.ContentType}'.");
            if (item.ExpectedStatus is not (DocumentStatus.Completed or DocumentStatus.NeedsReview or DocumentStatus.Failed))
                errors.Add($"{item.Id}: expected status must be Completed, NeedsReview, or Failed.");
            if (!Enum.IsDefined(item.ExpectedType)) errors.Add($"{item.Id}: invalid expected type.");
            if (!Enum.IsDefined(item.ExpectedDepartment)) errors.Add($"{item.Id}: invalid expected department.");
            if (item.ExpectedStatus is DocumentStatus.NeedsReview or DocumentStatus.Failed &&
                item.ExpectedDepartment != Department.Unassigned)
                errors.Add($"{item.Id}: {item.ExpectedStatus} documents must have expected department Unassigned.");
            foreach (var field in item.ExpectedFields.Keys)
            {
                if (!SupportedFields.Contains(field)) errors.Add($"{item.Id}: unsupported expected field '{field}'.");
                else if (item.ExpectedType != DocumentType.Unknown && !DocumentFieldSchema.GetSupportedFieldNames(item.ExpectedType).Contains(field))
                    errors.Add($"{item.Id}: field '{field}' is not supported for {item.ExpectedType}.");
            }
        }

        return errors;
    }
}
