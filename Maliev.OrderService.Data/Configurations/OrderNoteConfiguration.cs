using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Maliev.OrderService.Data.Models;

namespace Maliev.OrderService.Data.Configurations;

public class OrderNoteConfiguration : IEntityTypeConfiguration<OrderNote>
{
    public void Configure(EntityTypeBuilder<OrderNote> builder)
    {
        builder.ToTable("order_notes");

        builder.HasKey(on => on.NoteId);
        builder.Property(on => on.NoteId).HasColumnName("note_id").ValueGeneratedOnAdd();
        builder.Property(on => on.OrderId).HasColumnName("order_id").HasMaxLength(50).IsRequired();
        builder.Property(on => on.NoteType).HasColumnName("note_type").HasMaxLength(20).IsRequired();
        builder.Property(on => on.NoteText).HasColumnName("note_text").HasColumnType("text").IsRequired();
        builder.Property(on => on.CreatedBy).HasColumnName("created_by").HasMaxLength(50).IsRequired();
        builder.Property(on => on.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(on => on.OrderId).HasDatabaseName("IX_OrderNote_OrderId");
        builder.HasIndex(on => on.NoteType).HasDatabaseName("IX_OrderNote_NoteType");
        builder.HasIndex(on => on.CreatedBy).HasDatabaseName("IX_OrderNote_CreatedBy");
        builder.HasIndex(on => on.CreatedAt).HasDatabaseName("IX_OrderNote_CreatedAt");
    }
}
