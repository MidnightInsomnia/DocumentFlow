using DocumentFlow.Domain.Enums;
using DocumentFlow.Infrastructure.Persistence;
using DocumentFlow.Infrastructure.ReviewWork;
using Microsoft.EntityFrameworkCore;

namespace DocumentFlow.Infrastructure.Workspace;

public sealed record WorkspaceWriteRequest(Guid? KindId, Guid? DepartmentId);
public sealed record WorkspaceResponse(Guid DocumentId, Guid? KindId, Guid? DepartmentId, string? KindName,
    string? DepartmentName, bool KindIsDraft, bool KindCreatedAutomatically, string? Resolution, string? ResolutionReason);

public sealed class DocumentWorkspaceService(DocumentFlowDbContext db, CatalogService catalogs)
{
    public async Task<WorkspaceResponse> GetAsync(Guid id, CancellationToken ct)
    {
        CatalogService.ValidId(id);
        var document = await db.Documents.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new { x.Type, x.TargetDepartment, x.IsArchived, x.ArchivedAtUtc }).SingleOrDefaultAsync(ct)
            ?? throw new WorkspaceException(404, "Документ не найден.");
        var workspace = await db.Set<DocumentWorkspace>().AsNoTracking().SingleOrDefaultAsync(x => x.DocumentId == id, ct);
        var kindId = workspace is null ? WorkspaceInitializer.KindId(document.Type) : workspace.KindId;
        var departmentId = workspace is null ? WorkspaceInitializer.DepartmentId(document.TargetDepartment) : workspace.DepartmentId;
        var kind = kindId.HasValue ? await db.Set<DocumentKind>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == kindId, ct) : null;
        var department = departmentId.HasValue ? await db.Set<OrganizationDepartment>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == departmentId, ct) : null;
        if (workspace is null)
        {
            // A read before background initialization must obey the same active-only defaults.
            if (kind is null || !kind.IsActive || kind.IsDraft) { kindId = null; kind = null; }
            if (department is null || !department.IsActive) { departmentId = null; department = null; }
        }
        var resolution = document.IsArchived ? workspace?.Resolution : null;
        var reason = document.IsArchived ? workspace?.ResolutionReason : null;
        if (document.IsArchived && resolution is null)
        {
            // ReviewWork records an immutable disposition in the same transaction as archiving.
            // Match this archive operation, not an old resolution from a restored document.
            var disposition = await db.Set<ReviewDisposition>().AsNoTracking()
                .Where(x => x.DocumentId == id && x.CreatedAtUtc == document.ArchivedAtUtc)
                .OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id).FirstOrDefaultAsync(ct);
            resolution = disposition?.Resolution;
            reason = disposition?.Reason;
        }
        return new(id, kindId, departmentId, kind?.Name, department?.Name, kind?.IsDraft ?? false,
            kind?.CreatedAutomatically ?? false, resolution, reason);
    }
    public async Task<WorkspaceResponse> SaveAsync(Guid id, WorkspaceWriteRequest request, CancellationToken ct)
    {
        CatalogService.ValidId(id);
        await using var transaction = await CatalogService.LockAsync(db, ct);
        // Row lock serializes metadata writes with archive/review processing transactions.
        var document = await db.Documents.FromSqlInterpolated($"SELECT * FROM \"Documents\" WHERE \"Id\" = {id} FOR UPDATE")
            .SingleOrDefaultAsync(ct) ?? throw new WorkspaceException(404, "Документ не найден.");
        if (document.IsArchived || document.Status is DocumentStatus.PendingProcessing or DocumentStatus.Processing)
            throw new WorkspaceException(409, "Нельзя менять назначения архивного или обрабатываемого документа.");
        var workspace = await db.Set<DocumentWorkspace>().SingleOrDefaultAsync(x => x.DocumentId == id, ct);
        if (request.KindId.HasValue)
        {
            CatalogService.ValidId(request.KindId.Value);
            if (!await db.Set<DocumentKind>().AnyAsync(x => x.Id == request.KindId && ((x.IsActive && !x.IsDraft) || (workspace != null && workspace.KindId == x.Id)), ct))
                throw new WorkspaceException(400, "Назначить можно только активный подтверждённый вид.");
        }
        if (request.DepartmentId != workspace?.DepartmentId)
            await catalogs.ValidateDepartmentAsync(request.DepartmentId, ct);
        if (workspace is null) { workspace = new DocumentWorkspace { DocumentId = id }; db.Add(workspace); }
        workspace.KindId = request.KindId;
        workspace.DepartmentId = request.DepartmentId;
        workspace.LastUpdatedAtUtc = DateTime.UtcNow;
        // Catalog metadata does not classify or confirm the original document. In particular Unknown stays Unknown.
        await db.SaveChangesAsync(ct);
        await db.Set<DocumentSearchIndex>().Where(x => x.DocumentId == id).ExecuteDeleteAsync(ct);
        await transaction.CommitAsync(ct);
        return await GetAsync(id, ct);
    }
}
