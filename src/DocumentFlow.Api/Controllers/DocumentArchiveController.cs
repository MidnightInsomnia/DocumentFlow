using DocumentFlow.Api.Responses.Documents;
using DocumentFlow.Application.Documents.Archive;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentFlow.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Operator")]
[Route("api/documents")]
public sealed class DocumentArchiveController(DocumentArchiveService service) : ControllerBase
{
    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType<DocumentArchiveResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken) =>
        ToResponse(await service.ArchiveAsync(id, cancellationToken));

    [HttpPost("{id:guid}/restore")]
    [ProducesResponseType<DocumentArchiveResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Restore(Guid id, CancellationToken cancellationToken) =>
        ToResponse(await service.RestoreAsync(id, cancellationToken));

    private IActionResult ToResponse(DocumentArchiveResult result) => result.Outcome switch
    {
        DocumentArchiveOutcome.Archived or DocumentArchiveOutcome.Restored => Ok(new DocumentArchiveResponse(
            result.DocumentId!.Value,
            result.Status!.Value.ToString(),
            result.IsArchived,
            result.ArchivedAtUtc,
            result.ArchivedByDisplayName)),
        DocumentArchiveOutcome.NotFound => NotFound(),
        DocumentArchiveOutcome.AlreadyArchived => BusinessConflict("document_already_archived", "Документ уже находится в архиве."),
        DocumentArchiveOutcome.NotArchived => BusinessConflict("document_not_archived", "Документ не находится в архиве."),
        DocumentArchiveOutcome.ProcessingInProgress => BusinessConflict("document_processing_in_progress",
            "Документ ожидает обработки или обрабатывается и не может быть архивирован."),
        _ => throw new InvalidOperationException("Unsupported archive result.")
    };

    private ConflictObjectResult BusinessConflict(string code, string detail)
    {
        var problem = new ProblemDetails
        {
            Title = "Document archive operation is unavailable",
            Detail = detail,
            Status = StatusCodes.Status409Conflict
        };
        problem.Extensions["code"] = code;
        return Conflict(problem);
    }
}
