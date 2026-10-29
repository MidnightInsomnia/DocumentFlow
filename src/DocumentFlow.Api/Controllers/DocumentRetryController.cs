using DocumentFlow.Api.Responses.Documents;
using DocumentFlow.Application.Documents.Retry;
using Microsoft.AspNetCore.Mvc;

namespace DocumentFlow.Api.Controllers;

[ApiController]
[Route("api/documents")]
public sealed class DocumentRetryController(DocumentRetryService retryService)
    : ControllerBase
{
    [HttpPost("{id:guid}/retry")]
    [ProducesResponseType<RetryDocumentResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Retry(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await retryService.RetryAsync(id, cancellationToken);

        return result.Outcome switch
        {
            RetryDocumentOutcome.Queued => Accepted(new RetryDocumentResponse(
                result.DocumentId!.Value,
                result.Status!.Value.ToString())),
            RetryDocumentOutcome.NotFound => NotFound(),
            RetryDocumentOutcome.NotRetryable => Conflict(new ProblemDetails
            {
                Title = "Document cannot be retried",
                Detail = result.ErrorMessage,
                Status = StatusCodes.Status409Conflict
            }),
            _ => throw new InvalidOperationException("Unsupported retry outcome.")
        };
    }
}
