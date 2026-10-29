using DocumentFlow.Application.Documents.Processing.Validation;
using DocumentFlow.Domain.Entities;

namespace DocumentFlow.Application.Abstractions.Processing;

public interface IDocumentValidator
{
    DocumentValidationResult Validate(Document document);
}
