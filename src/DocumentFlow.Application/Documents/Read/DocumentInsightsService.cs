using DocumentFlow.Application.Abstractions.Persistence;

namespace DocumentFlow.Application.Documents.Read;

public sealed class DocumentInsightsService(IDocumentInsightsRepository repository)
{
    public Task<RelatedDocumentsResult?> GetRelatedAsync(Guid documentId, CancellationToken cancellationToken) =>
        repository.GetRelatedAsync(documentId, cancellationToken);

    public Task<CounterpartyCard?> GetCounterpartyAsync(
        string inn, int page, int pageSize, CancellationToken cancellationToken) =>
        repository.GetCounterpartyAsync(inn, page, pageSize, cancellationToken);

    public Task<PotentialDuplicateResult?> GetPotentialDuplicatesAsync(Guid documentId, CancellationToken cancellationToken) =>
        repository.GetPotentialDuplicatesAsync(documentId, cancellationToken);

    public Task<ConsistencyCheckResult?> GetConsistencyAsync(Guid documentId, CancellationToken cancellationToken) =>
        repository.GetConsistencyAsync(documentId, cancellationToken);
}
