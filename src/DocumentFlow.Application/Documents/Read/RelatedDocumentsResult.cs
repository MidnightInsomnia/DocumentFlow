using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Application.Documents.Read;

public sealed record RelatedDocumentItem(
    Guid Id,
    string FileName,
    DocumentType Type,
    DocumentStatus Status,
    string? Number,
    string? Date,
    string? Amount,
    bool IsArchived);

public sealed record RelatedDocumentsResult(
    Guid DocumentId,
    DocumentType Type,
    string? ReferenceNumber,
    IReadOnlyList<RelatedDocumentItem> Items);
