using DocumentFlow.Infrastructure.ReviewWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/documents/{id:guid}")]
public sealed class DocumentReviewWorkController(ReviewWorkService service) : ControllerBase
{
    // No draft is JSON null (200). A stale draft retains its original baseVersion, never silently rebased.
    [HttpGet("review/draft")]
    public Task<IActionResult> GetDraft(Guid id, CancellationToken ct) =>
        Run(async () => await service.GetDraftAsync(id, ct));

    [HttpPut("review/draft")]
    public Task<IActionResult> SaveDraft(Guid id, [FromBody] ReviewDraftRequest request, CancellationToken ct) =>
        Run(async () => await service.SaveDraftAsync(id, request, ct));

    [HttpPost("reopen")]
    public Task<IActionResult> Reopen(Guid id, [FromBody] ReopenDocumentRequest request, CancellationToken ct) =>
        Run(async () => await service.ReopenAsync(id, request, ct));

    [HttpPost("resolve")]
    public Task<IActionResult> Resolve(Guid id, [FromBody] ResolveDocumentRequest request, CancellationToken ct) =>
        Run(async () => await service.ResolveAsync(id, request, ct));

    [HttpGet("review/history")]
    public Task<IActionResult> History(Guid id, CancellationToken ct) =>
        Run(async () => await service.GetHistoryAsync(id, ct));

    private async Task<IActionResult> Run(Func<Task<object?>> operation)
    {
        try
        {
            var result = await operation();
            return result is null ? new JsonResult(null) { StatusCode = StatusCodes.Status200OK } : Ok(result);
        }
        catch (ReviewWorkException error)
        {
            var problem = new ProblemDetails
            {
                Title = "Операция с документом не выполнена", Detail = error.Message, Status = error.Status
            };
            problem.Extensions["code"] = error.Code;
            return StatusCode(error.Status, problem);
        }
    }
}
