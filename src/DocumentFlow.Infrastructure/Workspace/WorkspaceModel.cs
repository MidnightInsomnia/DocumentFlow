using DocumentFlow.Domain.Entities;
using DocumentFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DocumentFlow.Infrastructure.Workspace;

public sealed class DocumentKind
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string NormalizedName { get; set; } = "";
    public DocumentType BaseType { get; set; }
    public bool IsDraft { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSystem { get; set; }
    public bool CreatedAutomatically { get; set; }
    public Guid? DepartmentId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class OrganizationDepartment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public bool IsSystem { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class DocumentWorkspace
{
    public Guid DocumentId { get; set; }
    public Guid? KindId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? Resolution { get; set; }
    public string? ResolutionReason { get; set; }
    public DateTime LastUpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class DocumentSearchIndex
{
    public Guid DocumentId { get; set; }
    public string Model { get; set; } = "";
    public string Fingerprint { get; set; } = "";
    public string EmbeddingJson { get; set; } = "[]";
    public DateTime IndexedAtUtc { get; set; }
}

// Discovery retries are independent of embeddings: text search and catalog CRUD need no AI.
public sealed class AiKindDiscoveryState
{
    public Guid DocumentId { get; set; }
    public string Fingerprint { get; set; } = "";
    public DateTime LastAttemptAtUtc { get; set; }
    public bool Succeeded { get; set; }
}

public static class WorkspaceModel
{
    public static void Configure(ModelBuilder builder)
    {
        builder.Entity<OrganizationDepartment>(e =>
        {
            e.ToTable("OrganizationDepartments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(160).IsRequired();
        });
        builder.Entity<DocumentKind>(e =>
        {
            e.ToTable("DocumentKinds");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(160).IsRequired();
            e.Property(x => x.NormalizedName).HasMaxLength(160).IsRequired();
            e.HasIndex(x => x.NormalizedName).IsUnique();
            e.Property(x => x.BaseType).HasConversion<string>().HasMaxLength(32);
            e.HasOne<OrganizationDepartment>().WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<DocumentWorkspace>(e =>
        {
            e.ToTable("DocumentWorkspace");
            e.HasKey(x => x.DocumentId);
            e.Property(x => x.Resolution).HasMaxLength(40);
            e.Property(x => x.ResolutionReason).HasMaxLength(2000);
            e.HasOne<Document>().WithOne().HasForeignKey<DocumentWorkspace>(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<DocumentKind>().WithMany().HasForeignKey(x => x.KindId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<OrganizationDepartment>().WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<DocumentSearchIndex>(e =>
        {
            e.ToTable("DocumentSearchIndex");
            e.HasKey(x => x.DocumentId);
            e.Property(x => x.Model).HasMaxLength(400).IsRequired();
            e.Property(x => x.Fingerprint).HasMaxLength(64).IsRequired();
            e.Property(x => x.EmbeddingJson).HasColumnType("jsonb").IsRequired();
            e.HasIndex(x => x.Model);
            e.HasOne<Document>().WithOne().HasForeignKey<DocumentSearchIndex>(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<AiKindDiscoveryState>(e =>
        {
            e.ToTable("AiKindDiscoveryState");
            e.HasKey(x => x.DocumentId);
            e.Property(x => x.Fingerprint).HasMaxLength(64).IsRequired();
            e.HasOne<Document>().WithOne().HasForeignKey<AiKindDiscoveryState>(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
