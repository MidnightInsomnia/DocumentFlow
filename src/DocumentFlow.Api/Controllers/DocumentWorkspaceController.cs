using DocumentFlow.Infrastructure.Workspace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/workspace/documents")]
public sealed class DocumentWorkspaceController(DocumentWorkspaceService workspace, KindDiscoveryService discovery) : WorkspaceControllerBase
{
    [HttpGet("{id}")]
    public Task<IActionResult> Get(Guid id, CancellationToken ct) => ExecuteAsync(() => workspace.GetAsync(id, ct));
    [HttpGet("{id}/kind-discovery")]
    public Task<IActionResult> Discovery(Guid id, CancellationToken ct) => ExecuteAsync(() => discovery.GetAsync(id, ct));

    [Authorize(Roles = "Admin")]
    [HttpPost("{id}/kind-discovery/retry")]
    public Task<IActionResult> RetryDiscovery(Guid id, CancellationToken ct) =>
        ExecuteAsync(() => discovery.RetryAsync(id, ct), successStatus: 202);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(Guid id, [FromBody] WorkspaceWriteRequest request, CancellationToken ct) =>
        ExecuteAsync(() => workspace.SaveAsync(id, request, ct));
}
