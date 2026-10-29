namespace DocumentFlow.Application.Documents.Read;

public sealed record ConsistencyWarning(
    string Code,
    string Message,
    Guid? RelatedDocumentId,
    string? ActValue,
    string? ContractValue);

public sealed record ConsistencyCheckResult(
    Guid DocumentId,
    IReadOnlyList<ConsistencyWarning> Warnings,
    string CheckStatus = "insufficientData",
    string? Reason = null,
    IReadOnlyList<string>? CheckedRules = null);
