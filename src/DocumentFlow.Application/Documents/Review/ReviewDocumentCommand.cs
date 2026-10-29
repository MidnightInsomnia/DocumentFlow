using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Application.Documents.Review;

public sealed record ReviewDocumentCommand(
    DocumentType Type,
    IReadOnlyCollection<ReviewDocumentField> Fields,
    Guid? ExpectedVersion = null);

public sealed record ReviewDocumentField(string Name, string? Value);
