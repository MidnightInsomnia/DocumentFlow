using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Application.Abstractions.Processing;

public interface IDocumentRouter
{
    Department Route(DocumentType documentType);
}
