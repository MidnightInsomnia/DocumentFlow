using DocumentFlow.Api.Responses.Documents;
using DocumentFlow.Application.Documents.Review;
using Microsoft.AspNetCore.Mvc;

namespace DocumentFlow.Api.Controllers;

[ApiController]
[Route("api/documents")]
public sealed class DocumentReviewContextController(
    DocumentReviewContextService reviewContextService) : ControllerBase
{
    [HttpGet("{id:guid}/review-context")]
    [ProducesResponseType<DocumentReviewContextResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await reviewContextService.GetAsync(id, cancellationToken);
        return result.Outcome switch
        {
            DocumentReviewContextOutcome.Success => Ok(new DocumentReviewContextResponse(
                result.DocumentId!.Value,
                result.Type!.Value.ToString(),
                result.ClassificationConfidence,
                result.Issues!.Select(issue => new ReviewValidationIssueResponse(
                    issue.Code,
                    issue.FieldName,
                    issue.Message)).ToList())),
            DocumentReviewContextOutcome.NotFound => NotFound(),
            DocumentReviewContextOutcome.NotReviewable => Conflict(new ProblemDetails
            {
                Title = "Document cannot be reviewed",
                Detail = "Only documents awaiting review have review context.",
                Status = StatusCodes.Status409Conflict
            }),
            _ => throw new InvalidOperationException("Unsupported review context outcome.")
        };
    }
}
