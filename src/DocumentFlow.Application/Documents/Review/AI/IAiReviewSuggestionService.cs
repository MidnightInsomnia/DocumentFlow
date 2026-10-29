namespace DocumentFlow.Application.Documents.Review.AI;

public interface IAiReviewSuggestionService
{
    Task<AiReviewSuggestionResult> GetAsync(Guid documentId, CancellationToken cancellationToken);
}
