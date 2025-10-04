using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Maliev.OrderService.Data.Models;

namespace Maliev.OrderService.Data.Configurations;

public class OrderStatusConfiguration : IEntityTypeConfiguration<OrderStatus>
{
    public void Configure(EntityTypeBuilder<OrderStatus> builder)
    {
        builder.ToTable("order_statuses");

        builder.HasKey(os => os.StatusId);
        builder.Property(os => os.StatusId).HasColumnName("status_id").ValueGeneratedOnAdd();
        builder.Property(os => os.OrderId).HasColumnName("order_id").HasMaxLength(50).IsRequired();
        builder.Property(os => os.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        builder.Property(os => os.InternalNotes).HasColumnName("internal_notes").HasColumnType("text");
        builder.Property(os => os.CustomerNotes).HasColumnName("customer_notes").HasColumnType("text");
        builder.Property(os => os.Timestamp).HasColumnName("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(os => os.UpdatedBy).HasColumnName("updated_by").HasMaxLength(50).IsRequired();

        builder.HasIndex(os => os.OrderId).HasDatabaseName("IX_OrderStatus_OrderId");
        builder.HasIndex(os => os.Status).HasDatabaseName("IX_OrderStatus_Status");
        builder.HasIndex(os => os.Timestamp).HasDatabaseName("IX_OrderStatus_Timestamp");
    }
}
