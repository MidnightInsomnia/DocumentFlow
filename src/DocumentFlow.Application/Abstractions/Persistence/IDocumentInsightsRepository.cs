using DocumentFlow.Application.Documents.Read;

namespace DocumentFlow.Application.Abstractions.Persistence;

public interface IDocumentInsightsRepository
{
    Task<RelatedDocumentsResult?> GetRelatedAsync(Guid documentId, CancellationToken cancellationToken);
    Task<CounterpartyCard?> GetCounterpartyAsync(string inn, int page, int pageSize, CancellationToken cancellationToken);
    Task<PotentialDuplicateResult?> GetPotentialDuplicatesAsync(Guid documentId, CancellationToken cancellationToken);
    Task<ConsistencyCheckResult?> GetConsistencyAsync(Guid documentId, CancellationToken cancellationToken);
}
