using DocumentFlow.Infrastructure.Workspace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentFlow.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/catalogs")]
public sealed class AdminCatalogsController(CatalogService catalogs) : WorkspaceControllerBase
{
    [HttpGet("kinds")]
    public Task<IActionResult> Kinds(CancellationToken ct) => ExecuteAsync(async () => (await catalogs.GetAsync(true, ct)).Kinds);
    [HttpPost("kinds")]
    public Task<IActionResult> CreateKind([FromBody] KindWriteRequest request, CancellationToken ct) =>
        ExecuteAsync(() => catalogs.SaveKindAsync(null, request, ct), 201);
    [HttpPut("kinds/{id}")]
    public Task<IActionResult> UpdateKind(Guid id, [FromBody] KindWriteRequest request, CancellationToken ct) =>
        ExecuteAsync(() => catalogs.SaveKindAsync(id, request, ct));
    [HttpDelete("kinds/{id}")]
    public Task<IActionResult> DeleteKind(Guid id, CancellationToken ct) => ExecuteAsync(() => catalogs.DeleteKindAsync(id, ct));
    [HttpPost("kinds/{id}/approve")]
    public Task<IActionResult> ApproveKind(Guid id,
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] KindWriteRequest? request,
        CancellationToken ct) => ExecuteAsync(() => catalogs.ApproveKindAsync(id, request, ct));
    [HttpGet("departments")]
    public Task<IActionResult> Departments(CancellationToken ct) => ExecuteAsync(async () => (await catalogs.GetAsync(true, ct)).Departments);
    [HttpPost("departments")]
    public Task<IActionResult> CreateDepartment([FromBody] DepartmentWriteRequest request, CancellationToken ct) =>
        ExecuteAsync(() => catalogs.SaveDepartmentAsync(null, request, ct), 201);
    [HttpPut("departments/{id}")]
    public Task<IActionResult> UpdateDepartment(Guid id, [FromBody] DepartmentWriteRequest request, CancellationToken ct) =>
        ExecuteAsync(() => catalogs.SaveDepartmentAsync(id, request, ct));
    [HttpDelete("departments/{id}")]
    public Task<IActionResult> DeleteDepartment(Guid id, CancellationToken ct) => ExecuteAsync(() => catalogs.DeleteDepartmentAsync(id, ct));
}
