using DocumentFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentFlow.Infrastructure.Persistence.Configurations;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_Documents_ClassificationConfidence",
                "\"ClassificationConfidence\" IS NULL OR (\"ClassificationConfidence\" >= 0 AND \"ClassificationConfidence\" <= 1)");
            tableBuilder.HasCheckConstraint(
                "CK_Documents_ContentHash",
                "\"ContentHash\" IS NULL OR \"ContentHash\" ~ '^[0-9a-f]{64}$'");
        });

        builder.HasKey(document => document.Id);
        builder.Property(document => document.Id)
            .ValueGeneratedNever();

        builder.Property(document => document.OriginalFileName)
            .HasMaxLength(255)
            .IsRequired();
        builder.Property(document => document.StoredFileName)
            .HasMaxLength(255)
            .IsRequired();
        builder.Property(document => document.ContentType)
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(document => document.FileSizeBytes).IsRequired();
        builder.Property(document => document.ContentHash).HasMaxLength(64);
        builder.Property(document => document.IsArchived).HasDefaultValue(false).IsRequired().IsConcurrencyToken();
        builder.Property(document => document.ArchivedAtUtc);
        builder.Property(document => document.ArchivedByUserId);
        builder.Property(document => document.ArchivedByDisplayName).HasMaxLength(200);
        // Server default also backfills existing documents with non-empty, independent versions
        // when the shared migration adds this column. Domain operations always send a new Guid.
        builder.Property(document => document.Version).IsRequired().IsConcurrencyToken()
            .HasDefaultValueSql("gen_random_uuid()");
        builder.Property(document => document.Status).IsRequired();
        builder.Property(document => document.Type).IsRequired();
        builder.Property(document => document.ExtractedText).HasColumnType("text");
        builder.Property(document => document.ClassificationConfidence)
            .HasPrecision(5, 4);
        builder.Property(document => document.TargetDepartment).IsRequired();
        builder.Property(document => document.ProcessingError).HasMaxLength(2000);
        builder.Property(document => document.CreatedAtUtc).IsRequired();

        builder.HasIndex(document => document.StoredFileName).IsUnique();
        builder.HasIndex(document => document.ContentHash)
            .IsUnique()
            .HasDatabaseName("UX_Documents_ContentHash")
            .HasFilter("\"ContentHash\" IS NOT NULL");
        builder.HasIndex(document => document.Status);
        builder.HasIndex(document => document.Type);
        builder.HasIndex(document => document.TargetDepartment);
        builder.HasIndex(document => document.CreatedAtUtc);

    }
}
