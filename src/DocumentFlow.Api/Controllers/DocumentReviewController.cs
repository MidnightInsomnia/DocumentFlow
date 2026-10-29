using DocumentFlow.Api.Requests.Documents;
using DocumentFlow.Api.Responses.Documents;
using DocumentFlow.Application.Documents.Review;
using DocumentFlow.Application.Documents.Review.AI;
using DocumentFlow.Application.Documents.Read;
using DocumentFlow.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace DocumentFlow.Api.Controllers;

[ApiController]
[Route("api/documents")]
public sealed class DocumentReviewController(
    DocumentReviewService reviewService,
    IAiReviewSuggestionService aiSuggestionService,
    DocumentActionAuditService actionAuditService,
    DocumentReadService readService)
    : ControllerBase
{
    [HttpPost("{id:guid}/review/ai-suggestion")]
    [ProducesResponseType<AiReviewSuggestionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> GetAiSuggestion(Guid id, CancellationToken cancellationToken)
    {
        var document = await readService.GetByIdAsync(id, cancellationToken);
        if (document?.IsArchived == true)
        {
            var conflict = Problem("AI suggestion is unavailable for this document",
                "Restore the archived document before requesting an AI suggestion.",
                StatusCodes.Status409Conflict);
            conflict.Extensions["code"] = "document_archived";
            return Conflict(conflict);
        }
        await actionAuditService.RecordAiSuggestionRequestedAsync(id, cancellationToken);
        var result = await aiSuggestionService.GetAsync(id, cancellationToken);
        return result.Outcome switch
        {
            AiReviewSuggestionOutcome.Success => Ok(new AiReviewSuggestionResponse(
                result.Suggestion!.SuggestedType.ToString(),
                result.Suggestion.ClassificationConfidence,
                result.Suggestion.Fields.Select(field => new AiReviewSuggestedFieldResponse(
                    field.Name, field.Value, field.Confidence)).ToArray())),
            AiReviewSuggestionOutcome.NotFound => NotFound(),
            AiReviewSuggestionOutcome.NotReviewable or AiReviewSuggestionOutcome.MissingExtractedText =>
                Conflict(Problem("AI suggestion is unavailable for this document", result.ErrorMessage!, StatusCodes.Status409Conflict)),
            AiReviewSuggestionOutcome.Disabled or AiReviewSuggestionOutcome.UpstreamUnavailable =>
                StatusCode(StatusCodes.Status503ServiceUnavailable,
                    Problem("AI-assisted review is unavailable", result.ErrorMessage!, StatusCodes.Status503ServiceUnavailable,
                        result.Outcome == AiReviewSuggestionOutcome.Disabled ? "ai_disabled" : "ai_unavailable")),
            AiReviewSuggestionOutcome.ModelUnavailable =>
                StatusCode(StatusCodes.Status503ServiceUnavailable,
                    Problem("AI model is unavailable", result.ErrorMessage!, StatusCodes.Status503ServiceUnavailable, "ai_model_unavailable")),
            AiReviewSuggestionOutcome.TimedOut =>
                StatusCode(StatusCodes.Status504GatewayTimeout,
                    Problem("AI request timed out", result.ErrorMessage!, StatusCodes.Status504GatewayTimeout, "ai_timeout")),
            AiReviewSuggestionOutcome.InvalidUpstreamResponse =>
                StatusCode(StatusCodes.Status502BadGateway,
                    Problem("AI suggestion could not be generated", result.ErrorMessage!, StatusCodes.Status502BadGateway, "ai_invalid_response")),
            _ => throw new InvalidOperationException("Unsupported AI suggestion outcome.")
        };
    }

    [HttpPost("{id:guid}/review")]
    [ProducesResponseType<ReviewDocumentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ReviewValidationFailedResponse>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Review(
        Guid id,
        [FromBody] ReviewDocumentRequest request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.ExpectedVersion, out var expectedVersion) || expectedVersion == Guid.Empty)
            return BadRequest(Problem("Не указана версия документа",
                "expectedVersion обязателен и должен содержать GUID версии из актуальной карточки.",
                StatusCodes.Status400BadRequest, "document_version_required"));
        if (request.Fields?.Any(field => field is null) == true)
            return BadRequest(Problem("Некорректные поля", "Массив fields не может содержать null.", 400));
        if (request.Type is null ||
            !Enum.GetNames<DocumentType>().Any(name => string.Equals(
                name,
                request.Type,
                StringComparison.OrdinalIgnoreCase)) ||
            !Enum.TryParse<DocumentType>(request.Type, true, out var type) ||
            type == DocumentType.Unknown ||
            request.Fields is null)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Document review request is invalid",
                Detail = "Type must be Invoice, Contract, or Act, and fields are required.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var result = await reviewService.ReviewAsync(
            id,
            new ReviewDocumentCommand(
                type,
                request.Fields.Select(field => new ReviewDocumentField(
                    field.Name ?? string.Empty,
                    field.Value)).ToList(), expectedVersion),
            cancellationToken);

        return result.Outcome switch
        {
            ReviewDocumentOutcome.Completed => Ok(new ReviewDocumentResponse(
                result.DocumentId!.Value,
                result.Status!.Value.ToString(),
                result.Type!.Value.ToString(),
                result.TargetDepartment!.Value.ToString())),
            ReviewDocumentOutcome.VersionConflict => Conflict(Problem(
                "Версия документа изменилась", result.ErrorMessage!,
                StatusCodes.Status409Conflict, "document_version_conflict")),
            ReviewDocumentOutcome.NotFound => NotFound(),
            ReviewDocumentOutcome.NotReviewable => Conflict(new ProblemDetails
            {
                Title = "Document cannot be reviewed",
                Detail = result.ErrorMessage,
                Status = StatusCodes.Status409Conflict
            }),
            ReviewDocumentOutcome.InvalidRequest => BadRequest(new ProblemDetails
            {
                Title = "Document review request is invalid",
                Detail = result.ErrorMessage,
                Status = StatusCodes.Status400BadRequest
            }),
            ReviewDocumentOutcome.ValidationFailed => UnprocessableEntity(
                new ReviewValidationFailedResponse(
                    "Document review validation failed",
                    result.Issues!.Select(issue => new ReviewValidationIssueResponse(
                        issue.Code,
                        issue.FieldName,
                        issue.Message)).ToList())),
            _ => throw new InvalidOperationException("Unsupported review outcome.")
        };
    }

    private static ProblemDetails Problem(string title, string detail, int status, string? code = null)
    {
        var problem = new ProblemDetails { Title = title, Detail = detail, Status = status };
        if (code is not null) problem.Extensions["code"] = code;
        return problem;
    }
}
