using DocumentFlow.Infrastructure.Workspace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/search")]
public sealed class DocumentSearchController(DocumentSearchService search) : WorkspaceControllerBase
{
    [HttpGet("documents")]
    public Task<IActionResult> Documents([FromQuery] DocumentSearchQuery query, CancellationToken ct) =>
        ExecuteAsync(() => search.SearchAsync(query, ct));
    [HttpGet("counterparties")]
    public Task<IActionResult> Counterparties([FromQuery] string? q, CancellationToken ct) =>
        ExecuteAsync(() => search.CounterpartiesAsync(q, ct));
    [Authorize(Roles = "Admin")]
    [HttpGet("/api/admin/search/status")]
    public Task<IActionResult> Status(CancellationToken ct) => ExecuteAsync(() => search.StatusAsync(ct));
    [Authorize(Roles = "Admin")]
    [HttpPost("/api/admin/search/reindex")]
    public Task<IActionResult> Reindex([FromQuery] bool rebuild = true, CancellationToken ct = default) =>
        ExecuteAsync(() => search.ReindexAsync(rebuild, ct), 202);
}
