using Maliev.OrderService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.OrderService.Infrastructure.Persistence.Configurations
{
    /// <summary>EF Core configuration for <see cref="OrderNote"/>.</summary>
    internal sealed class OrderNoteConfiguration : IEntityTypeConfiguration<OrderNote>
    {
        public void Configure(EntityTypeBuilder<OrderNote> builder)
        {
            _ = builder.ToTable("order_notes");

            _ = builder.HasKey(on => on.NoteId);
            _ = builder.Property(on => on.NoteId).HasColumnName("note_id").ValueGeneratedOnAdd();
            _ = builder.Property(on => on.OrderId).HasColumnName("order_id").HasMaxLength(50).IsRequired();
            _ = builder.Property(on => on.NoteType).HasColumnName("note_type").HasMaxLength(20).IsRequired();
            _ = builder.Property(on => on.NoteText).HasColumnName("note_text").HasColumnType("text").IsRequired();
            _ = builder.Property(on => on.CreatedBy).HasColumnName("created_by").HasMaxLength(50).IsRequired();
            _ = builder.Property(on => on.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            _ = builder.HasIndex(on => on.OrderId).HasDatabaseName("IX_OrderNote_OrderId");
            _ = builder.HasIndex(on => on.NoteType).HasDatabaseName("IX_OrderNote_NoteType");
            _ = builder.HasIndex(on => on.CreatedBy).HasDatabaseName("IX_OrderNote_CreatedBy");
            _ = builder.HasIndex(on => on.CreatedAt).HasDatabaseName("IX_OrderNote_CreatedAt");
        }
    }
}
