using DocumentFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentFlow.Infrastructure.Persistence.Configurations;

public sealed class DocumentFieldConfiguration : IEntityTypeConfiguration<DocumentField>
{
    public void Configure(EntityTypeBuilder<DocumentField> builder)
    {
        builder.ToTable("DocumentFields", tableBuilder =>
            tableBuilder.HasCheckConstraint(
                "CK_DocumentFields_Confidence",
                "\"Confidence\" IS NULL OR (\"Confidence\" >= 0 AND \"Confidence\" <= 1)"));

        builder.HasKey(field => field.Id);
        builder.Property(field => field.Id)
            .ValueGeneratedNever();

        builder.Property(field => field.Name)
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(field => field.Value).HasColumnType("text");
        builder.Property(field => field.Confidence).HasPrecision(5, 4);
        builder.Property(field => field.WasManuallyEdited).IsRequired();
        builder.Property(field => field.CreatedAtUtc).IsRequired();

        builder.HasOne(field => field.Document)
            .WithMany(document => document.Fields)
            .HasForeignKey(field => field.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(field => new { field.DocumentId, field.Name }).IsUnique();
    }
}
