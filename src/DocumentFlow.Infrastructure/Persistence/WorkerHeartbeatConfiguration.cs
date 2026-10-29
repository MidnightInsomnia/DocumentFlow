using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentFlow.Infrastructure.Persistence;

public sealed class WorkerHeartbeatConfiguration : IEntityTypeConfiguration<WorkerHeartbeat>
{
    public void Configure(EntityTypeBuilder<WorkerHeartbeat> entity)
    {
        entity.ToTable("WorkerHeartbeats");
        entity.HasKey(heartbeat => heartbeat.InstanceId);
        entity.Property(heartbeat => heartbeat.InstanceId).HasMaxLength(200);
        entity.Property(heartbeat => heartbeat.Version).HasMaxLength(100).IsRequired();
        entity.Property(heartbeat => heartbeat.StartedAtUtc).IsRequired();
        entity.Property(heartbeat => heartbeat.LastSeenUtc).IsRequired();
    }
}
