using System.Text;
using System.Text.RegularExpressions;
using DocumentFlow.Domain.Enums;
using DocumentFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DocumentFlow.Infrastructure.Workspace;

public sealed class WorkspaceException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed record KindWriteRequest(string Name, string BaseType, Guid? DepartmentId = null, bool? IsActive = null);
public sealed record DepartmentWriteRequest(string Name, bool? IsActive = null);
public sealed record KindResponse(Guid Id, string Name, string BaseType, bool IsDraft, bool IsActive, bool IsSystem,
    bool CreatedAutomatically, Guid? DepartmentId);
public sealed record DepartmentResponse(Guid Id, string Name, bool IsActive, bool IsSystem);
public sealed record CatalogResponse(IReadOnlyList<KindResponse> Kinds, IReadOnlyList<DepartmentResponse> Departments);
public sealed record CatalogDeleteResponse(Guid Id, bool Deleted, bool Deactivated, string Message);

public sealed class CatalogService(DocumentFlowDbContext db)
{
    public static string Normalize(string value) => Regex.Replace(value.Normalize(NormalizationForm.FormKC).Trim(), @"\s+", " ").ToUpperInvariant().Replace('Ё', 'Е');
    public static string ValidName(string? value)
    {
        var name = value?.Normalize(NormalizationForm.FormKC).Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 160 || name.Any(char.IsControl))
            throw new WorkspaceException(400, "Название обязательно: от 1 до 160 символов, без управляющих знаков.");
        return Regex.Replace(name, @"\s+", " ");
    }
    public static DocumentType ValidBaseType(string? value)
    {
        if (!Enum.GetNames<DocumentType>().Any(x => string.Equals(x, value, StringComparison.OrdinalIgnoreCase)) ||
            !Enum.TryParse<DocumentType>(value, true, out var type))
            throw new WorkspaceException(400, "baseType должен быть Unknown, Invoice, Contract или Act (техническая схема).");
        return type;
    }
    public static void ValidId(Guid id)
    {
        if (id == Guid.Empty) throw new WorkspaceException(400, "Пустой идентификатор недопустим.");
    }
    internal static async Task<IDbContextTransaction> LockAsync(DocumentFlowDbContext db, CancellationToken ct)
    {
        var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(6492247145232)", ct);
            return transaction;
        }
        catch { await transaction.DisposeAsync(); throw; }
    }
    public static KindResponse ToResponse(DocumentKind kind) => new(kind.Id, kind.Name, kind.BaseType.ToString(),
        kind.IsDraft, kind.IsActive, kind.IsSystem, kind.CreatedAutomatically, kind.DepartmentId);
    public static DepartmentResponse ToResponse(OrganizationDepartment department) =>
        new(department.Id, department.Name, department.IsActive, department.IsSystem);

    public async Task<CatalogResponse> GetAsync(bool admin, CancellationToken ct)
    {
        var kinds = await db.Set<DocumentKind>().AsNoTracking().Where(x => admin || (x.IsActive && !x.IsDraft))
            .OrderBy(x => x.Name).ToListAsync(ct);
        var departments = await db.Set<OrganizationDepartment>().AsNoTracking().Where(x => admin || x.IsActive)
            .OrderBy(x => x.Name).ToListAsync(ct);
        return new(kinds.Select(ToResponse).ToArray(), departments.Select(ToResponse).ToArray());
    }
    internal async Task ValidateDepartmentAsync(Guid? id, CancellationToken ct)
    {
        if (id is null) return;
        ValidId(id.Value);
        if (!await db.Set<OrganizationDepartment>().AnyAsync(x => x.Id == id && x.IsActive, ct))
            throw new WorkspaceException(400, "Выберите существующий активный отдел.");
    }
    public async Task<KindResponse> SaveKindAsync(Guid? id, KindWriteRequest request, CancellationToken ct)
    {
        if (id.HasValue) ValidId(id.Value);
        var name = ValidName(request.Name);
        var normalized = Normalize(name);
        var type = ValidBaseType(request.BaseType);
        await using var transaction = await LockAsync(db, ct);
        await ValidateDepartmentAsync(request.DepartmentId, ct);
        var kind = id.HasValue ? await db.Set<DocumentKind>().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new WorkspaceException(404, "Вид не найден.") : new DocumentKind();
        if (kind.IsSystem && type != kind.BaseType)
            throw new WorkspaceException(409, "Техническая схема системного вида неизменна. Создайте новый пользовательский вид.");
        if (await db.Set<DocumentKind>().AnyAsync(x => x.NormalizedName == normalized && x.Id != kind.Id, ct))
            throw new WorkspaceException(409, "Вид с таким названием уже существует, включая неактивные и черновики.");
        kind.Name = name;
        kind.NormalizedName = normalized;
        kind.BaseType = type;
        kind.DepartmentId = request.DepartmentId;
        // Editing an AI suggestion must never implicitly approve it.
        kind.IsActive = !kind.IsDraft && (request.IsActive ?? (id.HasValue ? kind.IsActive : true));
        if (!id.HasValue) db.Add(kind);
        await db.SaveChangesAsync(ct);
        await db.Set<DocumentSearchIndex>().Where(x => db.Set<DocumentWorkspace>().Any(w => w.DocumentId == x.DocumentId && w.KindId == kind.Id)).ExecuteDeleteAsync(ct);
        await transaction.CommitAsync(ct);
        return ToResponse(kind);
    }
    public async Task<KindResponse> ApproveKindAsync(Guid id, KindWriteRequest? request, CancellationToken ct)
    {
        ValidId(id);
        await using var transaction = await LockAsync(db, ct);
        var kind = await db.Set<DocumentKind>().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new WorkspaceException(404, "Вид не найден.");
        if (request is not null)
        {
            var name = ValidName(request.Name);
            var normalized = Normalize(name);
            var type = ValidBaseType(request.BaseType);
            if (kind.IsSystem && kind.BaseType != type)
                throw new WorkspaceException(409, "Техническая схема системного вида неизменна.");
            if (await db.Set<DocumentKind>().AnyAsync(x => x.Id != id && x.NormalizedName == normalized, ct))
                throw new WorkspaceException(409, "Вид с таким названием уже существует.");
            await ValidateDepartmentAsync(request.DepartmentId, ct);
            kind.Name = name;
            kind.NormalizedName = normalized;
            kind.BaseType = type;
            kind.DepartmentId = request.DepartmentId;
        }
        await ValidateDepartmentAsync(kind.DepartmentId, ct);
        kind.IsDraft = false;
        kind.IsActive = true;
        await db.SaveChangesAsync(ct);
        await db.Set<DocumentSearchIndex>().Where(x => db.Set<DocumentWorkspace>().Any(w => w.DocumentId == x.DocumentId && w.KindId == kind.Id)).ExecuteDeleteAsync(ct);
        await transaction.CommitAsync(ct);
        return ToResponse(kind);
    }
    public async Task<DepartmentResponse> SaveDepartmentAsync(Guid? id, DepartmentWriteRequest request, CancellationToken ct)
    {
        if (id.HasValue) ValidId(id.Value);
        var name = ValidName(request.Name);
        var normalized = Normalize(name);
        await using var transaction = await LockAsync(db, ct);
        var department = id.HasValue ? await db.Set<OrganizationDepartment>().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new WorkspaceException(404, "Отдел не найден.") : new OrganizationDepartment();
        var others = await db.Set<OrganizationDepartment>().AsNoTracking().Where(x => x.Id != department.Id)
            .Select(x => x.Name).ToListAsync(ct);
        if (others.Any(x => Normalize(x) == normalized)) throw new WorkspaceException(409, "Отдел с таким названием уже существует.");
        department.Name = name;
        department.IsActive = request.IsActive ?? (id.HasValue ? department.IsActive : true);
        if (!id.HasValue) db.Add(department);
        await db.SaveChangesAsync(ct);
        await db.Set<DocumentSearchIndex>().Where(x => db.Set<DocumentWorkspace>().Any(w => w.DocumentId == x.DocumentId && w.DepartmentId == department.Id)).ExecuteDeleteAsync(ct);
        await transaction.CommitAsync(ct);
        return ToResponse(department);
    }
    public async Task<CatalogDeleteResponse> DeleteKindAsync(Guid id, CancellationToken ct)
    {
        ValidId(id);
        await using var transaction = await LockAsync(db, ct);
        var kind = await db.Set<DocumentKind>().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new WorkspaceException(404, "Вид не найден.");
        var used = await db.Set<DocumentWorkspace>().AnyAsync(x => x.KindId == id, ct);
        if (used || kind.IsSystem) kind.IsActive = false;
        else db.Remove(kind);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new(id, !used && !kind.IsSystem, used || kind.IsSystem,
            used || kind.IsSystem ? "Вид деактивирован; история и назначения документов сохранены." : "Неиспользуемый вид удалён.");
    }
    public async Task<CatalogDeleteResponse> DeleteDepartmentAsync(Guid id, CancellationToken ct)
    {
        ValidId(id);
        await using var transaction = await LockAsync(db, ct);
        var department = await db.Set<OrganizationDepartment>().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new WorkspaceException(404, "Отдел не найден.");
        var used = await db.Set<DocumentWorkspace>().AnyAsync(x => x.DepartmentId == id, ct) ||
            await db.Set<DocumentKind>().AnyAsync(x => x.DepartmentId == id, ct);
        if (used || department.IsSystem) department.IsActive = false;
        else db.Remove(department);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new(id, !used && !department.IsSystem, used || department.IsSystem,
            used || department.IsSystem ? "Отдел деактивирован; история и назначения документов сохранены." : "Неиспользуемый отдел удалён.");
    }
}
