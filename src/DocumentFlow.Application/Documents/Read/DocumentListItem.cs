using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Application.Documents.Read;

public sealed record DocumentListItem(
    Guid Id,
    string FileName,
    DocumentStatus Status,
    DocumentType Type,
    decimal? ClassificationConfidence,
    Department TargetDepartment,
    DateTime CreatedAtUtc,
    DateTime? ProcessedAtUtc,
    bool IsArchived = false,
    string? MatchReason = null,
    string? Number = null,
    string? Date = null,
    string? Inn = null,
    string? Amount = null,
    string? CustomerINN = null,
    string? ContractorINN = null,
    string? SupplierINN = null,
    string? BuyerINN = null);
