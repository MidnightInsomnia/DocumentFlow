using DocumentFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocumentFlow.Infrastructure.ReviewWork;

public static class ReviewWorkModel
{
    public static void Configure(ModelBuilder builder)
    {
        builder.Entity<ReviewDraft>(entity =>
        {
            entity.ToTable("ReviewDrafts");
            entity.HasKey(item => new { item.DocumentId, item.UserId });
            entity.Property(item => item.FieldsJson).HasColumnType("jsonb").IsRequired();
            entity.Property(item => item.Type).IsRequired();
            entity.Property(item => item.BaseVersion).IsRequired();
            entity.Property(item => item.UpdatedAtUtc).IsRequired();
            entity.HasOne<Document>().WithMany().HasForeignKey(item => item.DocumentId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<ReviewRevision>(entity =>
        {
            entity.ToTable("ReviewRevisions");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).ValueGeneratedNever();
            entity.Property(item => item.FieldsJson).HasColumnType("jsonb").IsRequired();
            entity.Property(item => item.Reason).HasMaxLength(1000).IsRequired();
            entity.Property(item => item.ActorDisplayName).HasMaxLength(200).IsRequired();
            entity.Property(item => item.PreviousReviewerDisplayName).HasMaxLength(200);
            entity.HasIndex(item => new { item.DocumentId, item.CreatedAtUtc });
            entity.HasOne<Document>().WithMany().HasForeignKey(item => item.DocumentId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<ReviewDisposition>(entity =>
        {
            entity.ToTable("ReviewDispositions");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).ValueGeneratedNever();
            entity.Property(item => item.Resolution).HasMaxLength(32).IsRequired();
            entity.Property(item => item.Reason).HasMaxLength(1000).IsRequired();
            entity.Property(item => item.ActorDisplayName).HasMaxLength(200).IsRequired();
            entity.HasIndex(item => new { item.DocumentId, item.CreatedAtUtc });
            entity.HasOne<Document>().WithMany().HasForeignKey(item => item.DocumentId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
