using DocumentFlow.Api.Responses.Documents;
using DocumentFlow.Application.Documents.Reprocess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentFlow.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Operator")]
[Route("api/documents")]
public sealed class DocumentReprocessController(DocumentReprocessService service) : ControllerBase
{
    [HttpPost("{id:guid}/reprocess")]
    [ProducesResponseType<ReprocessDocumentResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reprocess(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.ReprocessAsync(id, cancellationToken);
        return result.Outcome switch
        {
            ReprocessDocumentOutcome.Queued => Accepted(new ReprocessDocumentResponse(
                result.DocumentId!.Value, result.Status!.Value.ToString())),
            ReprocessDocumentOutcome.NotFound => NotFound(),
            ReprocessDocumentOutcome.Archived => ConflictWithCode("document_reprocess_archived",
                "Сначала восстановите документ из архива."),
            ReprocessDocumentOutcome.ProcessingInProgress => ConflictWithCode("document_reprocess_in_progress",
                "Документ уже ожидает обработки или обрабатывается."),
            ReprocessDocumentOutcome.ManualEdits => ConflictWithCode("document_reprocess_manual_edits",
                "Документ содержит ручные изменения. Повторная обработка без их сохранения запрещена."),
            ReprocessDocumentOutcome.SourceMissing => ConflictWithCode("document_reprocess_source_missing",
                "Исходный файл документа отсутствует в хранилище."),
            _ => throw new InvalidOperationException("Unsupported reprocess outcome.")
        };
    }

    private ConflictObjectResult ConflictWithCode(string code, string detail)
    {
        var problem = new ProblemDetails
        {
            Title = "Document cannot be reprocessed",
            Detail = detail,
            Status = StatusCodes.Status409Conflict
        };
        problem.Extensions["code"] = code;
        return Conflict(problem);
    }
}
