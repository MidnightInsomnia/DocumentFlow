using DocumentFlow.Domain.Entities;

namespace DocumentFlow.Application.Abstractions.Processing;

public interface ITextExtractorResolver
{
    ITextExtractor Resolve(Document document);
}
