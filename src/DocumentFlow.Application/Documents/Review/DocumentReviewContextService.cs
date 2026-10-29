using DocumentFlow.Application.Abstractions.Persistence;
using DocumentFlow.Application.Abstractions.Processing;
using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Application.Documents.Review;

public sealed class DocumentReviewContextService(
    IDocumentReviewContextRepository repository,
    IDocumentValidator validator)
{
    public async Task<DocumentReviewContextResult> GetAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var document = await repository.GetByIdAsync(id, cancellationToken);
        if (document is null)
        {
            return new DocumentReviewContextResult(DocumentReviewContextOutcome.NotFound);
        }

        if (document.IsArchived || document.Status != DocumentStatus.NeedsReview)
        {
            return new DocumentReviewContextResult(
                DocumentReviewContextOutcome.NotReviewable,
                document.Id,
                document.Type,
                document.ClassificationConfidence);
        }

        var validation = validator.Validate(document);
        return new DocumentReviewContextResult(
            DocumentReviewContextOutcome.Success,
            document.Id,
            document.Type,
            document.ClassificationConfidence,
            validation.Issues);
    }
}
