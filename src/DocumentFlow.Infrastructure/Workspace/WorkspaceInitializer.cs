using DocumentFlow.Domain.Enums;
using DocumentFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DocumentFlow.Infrastructure.Workspace;

public sealed class WorkspaceInitializer(DocumentFlowDbContext db)
{
    public static readonly Guid AccountingId = Guid.Parse("946f56e2-411f-4c3a-a741-000000000001");
    public static readonly Guid LegalId = Guid.Parse("946f56e2-411f-4c3a-a741-000000000002");
    public static readonly Guid InvoiceId = Guid.Parse("946f56e2-411f-4c3a-a742-000000000001");
    public static readonly Guid ContractId = Guid.Parse("946f56e2-411f-4c3a-a742-000000000002");
    public static readonly Guid ActId = Guid.Parse("946f56e2-411f-4c3a-a742-000000000003");
    public static Guid? KindId(DocumentType type) => type switch
    {
        DocumentType.Invoice => InvoiceId, DocumentType.Contract => ContractId, DocumentType.Act => ActId, _ => null
    };
    public static Guid? DepartmentId(Department department) => department switch
    {
        Department.Accounting => AccountingId, Department.Legal => LegalId, _ => null
    };
    internal static async Task<(HashSet<Guid> Kinds, HashSet<Guid> Departments)> ActiveDefaultsAsync(DocumentFlowDbContext db, CancellationToken ct)
    {
        var kinds = await db.Set<DocumentKind>().AsNoTracking().Where(x => x.IsActive && !x.IsDraft).Select(x => x.Id).ToListAsync(ct);
        var departments = await db.Set<OrganizationDepartment>().AsNoTracking().Where(x => x.IsActive).Select(x => x.Id).ToListAsync(ct);
        return (kinds.ToHashSet(), departments.ToHashSet());
    }
    internal static DocumentWorkspace NewWorkspace(Guid id, DocumentType type, Department department,
        (HashSet<Guid> Kinds, HashSet<Guid> Departments) active) => new()
    {
        DocumentId = id,
        KindId = KindId(type) is { } kindId && active.Kinds.Contains(kindId) ? kindId : null,
        DepartmentId = DepartmentId(department) is { } departmentId && active.Departments.Contains(departmentId) ? departmentId : null
    };
    // Run once after Database.MigrateAsync(), in a service scope; never runs migrations itself.
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using (var transaction = await CatalogService.LockAsync(db, ct))
        {
            foreach (var (id, name) in new[] { (AccountingId, "Бухгалтерия"), (LegalId, "Юридический отдел") })
                if (!await db.Set<OrganizationDepartment>().AnyAsync(x => x.Id == id, ct))
                    db.Add(new OrganizationDepartment { Id = id, Name = name, IsSystem = true });
            foreach (var (id, name, type, departmentId) in new[]
                     { (InvoiceId, "Счёт", DocumentType.Invoice, AccountingId),
                       (ContractId, "Договор", DocumentType.Contract, LegalId),
                       (ActId, "Акт", DocumentType.Act, AccountingId) })
                if (!await db.Set<DocumentKind>().AnyAsync(x => x.Id == id, ct))
                    db.Add(new DocumentKind { Id = id, Name = name, NormalizedName = CatalogService.Normalize(name),
                        BaseType = type, DepartmentId = departmentId, IsSystem = true });
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        while (true)
        {
            await using var transaction = await CatalogService.LockAsync(db, ct);
            var documents = await db.Documents.AsNoTracking()
                .Where(x => x.Status != DocumentStatus.PendingProcessing && x.Status != DocumentStatus.Processing &&
                    !db.Set<DocumentWorkspace>().Any(w => w.DocumentId == x.Id))
                .OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id).Take(100)
                .Select(x => new { x.Id, x.Type, x.TargetDepartment }).ToListAsync(ct);
            if (documents.Count == 0) break;
            var active = await ActiveDefaultsAsync(db, ct);
            foreach (var d in documents)
                db.Add(NewWorkspace(d.Id, d.Type, d.TargetDepartment, active));
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            db.ChangeTracker.Clear();
        }
    }
}
