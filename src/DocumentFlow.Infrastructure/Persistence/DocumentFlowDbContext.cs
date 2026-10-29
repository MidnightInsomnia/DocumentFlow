using DocumentFlow.Domain.Entities;
using DocumentFlow.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DocumentFlow.Infrastructure.Persistence;

public sealed class DocumentFlowDbContext(DbContextOptions<DocumentFlowDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentField> DocumentFields => Set<DocumentField>();
    public DbSet<DocumentEvent> DocumentEvents => Set<DocumentEvent>();
    public DbSet<WorkerHeartbeat> WorkerHeartbeats => Set<WorkerHeartbeat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(user => user.IsActive).IsRequired();
            entity.Property(user => user.CreatedAtUtc).IsRequired();
        });
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(DocumentFlowDbContext).Assembly);
        DocumentFlow.Infrastructure.Workspace.WorkspaceModel.Configure(modelBuilder);
        DocumentFlow.Infrastructure.ReviewWork.ReviewWorkModel.Configure(modelBuilder);
    }
}
