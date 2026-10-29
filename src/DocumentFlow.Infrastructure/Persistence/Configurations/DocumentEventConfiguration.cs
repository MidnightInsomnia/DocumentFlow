using DocumentFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentFlow.Infrastructure.Persistence.Configurations;

public sealed class DocumentEventConfiguration : IEntityTypeConfiguration<DocumentEvent>
{
    public void Configure(EntityTypeBuilder<DocumentEvent> builder)
    {
        builder.ToTable("DocumentEvents");
        builder.HasKey(documentEvent => documentEvent.Id);
        builder.Property(documentEvent => documentEvent.Id)
            .ValueGeneratedNever();

        builder.Property(documentEvent => documentEvent.Type).IsRequired();
        builder.Property(documentEvent => documentEvent.Message).HasMaxLength(2000);
        builder.Property(documentEvent => documentEvent.ActorDisplayName).HasMaxLength(256);
        builder.Property(documentEvent => documentEvent.CreatedAtUtc).IsRequired();

        builder.HasOne(documentEvent => documentEvent.Document)
            .WithMany(document => document.Events)
            .HasForeignKey(documentEvent => documentEvent.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(documentEvent => new
        {
            documentEvent.DocumentId,
            documentEvent.CreatedAtUtc
        });
    }
}
