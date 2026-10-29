using DocumentFlow.Api.Responses.Documents;
using DocumentFlow.Application.Documents.Read;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/documents")]
public sealed class DocumentInsightsController(DocumentInsightsService service) : ControllerBase
{
    [HttpGet("{id:guid}/consistency-checks")]
    [ProducesResponseType<ConsistencyChecksResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConsistencyChecks(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetConsistencyAsync(id, cancellationToken);
        if (result is null) return NotFound();
        return Ok(new ConsistencyChecksResponse(result.DocumentId,
            result.Warnings.Select(warning => new ConsistencyWarningResponse(
                warning.Code, warning.Message, warning.RelatedDocumentId,
                warning.ActValue, warning.ContractValue)).ToList(), result.CheckStatus, result.Reason, result.CheckedRules));
    }

    [HttpGet("{id:guid}/potential-duplicates")]
    [ProducesResponseType<PotentialDuplicatesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPotentialDuplicates(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetPotentialDuplicatesAsync(id, cancellationToken);
        if (result is null) return NotFound();
        return Ok(new PotentialDuplicatesResponse(result.SourceDocumentId, result.Supported,
            result.MatchKey is null ? null : new BusinessDuplicateMatchKeyResponse(
                result.MatchKey.Inn, result.MatchKey.Number, result.MatchKey.Date),
            result.Reason, result.SourceAmount,
            result.Items.Select(item => new PotentialDuplicateItemResponse(
                item.Id, item.FileName, item.Type.ToString(), item.Status.ToString(),
                item.Number, item.Date, item.Inn, item.Amount, item.IsArchived,
                item.CreatedAtUtc, item.AmountDiffers)).ToList()));
    }

    [HttpGet("{id:guid}/related")]
    [ProducesResponseType<RelatedDocumentsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRelated(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetRelatedAsync(id, cancellationToken);
        if (result is null) return NotFound();
        return Ok(new RelatedDocumentsResponse(result.DocumentId, result.Type.ToString(),
            result.ReferenceNumber, result.Items.Select(item => new RelatedDocumentResponse(
                item.Id, item.FileName, item.Type.ToString(), item.Status.ToString(),
                item.Number, item.Date, item.Amount, item.IsArchived)).ToList()));
    }
}

[ApiController]
[Authorize]
[Route("api/counterparties")]
public sealed class CounterpartiesController(DocumentInsightsService service) : ControllerBase
{
    [HttpGet("{inn}")]
    [ProducesResponseType<CounterpartyCardResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByInn(
        string inn, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (inn.Length is not (10 or 12) || !inn.All(char.IsAsciiDigit) ||
            page < 1 || pageSize is < 1 or > 100 || page > int.MaxValue / pageSize)
            return BadRequest(new ProblemDetails { Title = "Invalid counterparty query",
                Detail = "INN must have 10 or 12 ASCII digits; page >= 1; pageSize between 1 and 100.",
                Status = StatusCodes.Status400BadRequest });

        var card = await service.GetCounterpartyAsync(inn, page, pageSize, cancellationToken);
        if (card is null) return NotFound();
        static CounterpartyDocumentResponse Map(CounterpartyDocumentItem item) => new(
            item.Id, item.FileName, item.Type.ToString(), item.Status.ToString(),
            item.Number, item.Kpp, item.Amount, item.CreatedAtUtc, item.IsArchived,
            item.CustomerINN, item.ContractorINN, item.SupplierINN, item.BuyerINN);
        return Ok(new CounterpartyCardResponse(card.Inn, card.KnownKpps, card.DocumentCount,
            card.CountsByType.ToDictionary(pair => pair.Key.ToString(), pair => pair.Value),
            card.CountsByStatus.ToDictionary(pair => pair.Key.ToString(), pair => pair.Value),
            card.NeedsReviewCount, card.KnownDocumentAmountTotal, card.AmountDocumentCount,
            card.LatestDocuments.Select(Map).ToList(), card.Documents.Select(Map).ToList(),
            card.Page, card.PageSize));
    }
}
