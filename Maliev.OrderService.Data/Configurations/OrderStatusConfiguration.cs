using Maliev.OrderService.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.OrderService.Data.Configurations
{
    public class OrderStatusConfiguration : IEntityTypeConfiguration<OrderStatus>
    {
        public void Configure(EntityTypeBuilder<OrderStatus> builder)
        {
            _ = builder.ToTable("order_statuses");

            _ = builder.HasKey(os => os.StatusId);
            _ = builder.Property(os => os.StatusId).HasColumnName("status_id").ValueGeneratedOnAdd();
            _ = builder.Property(os => os.OrderId).HasColumnName("order_id").HasMaxLength(50).IsRequired();
            _ = builder.Property(os => os.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
            _ = builder.Property(os => os.InternalNotes).HasColumnName("internal_notes").HasColumnType("text");
            _ = builder.Property(os => os.CustomerNotes).HasColumnName("customer_notes").HasColumnType("text");
            _ = builder.Property(os => os.Timestamp).HasColumnName("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");
            _ = builder.Property(os => os.UpdatedBy).HasColumnName("updated_by").HasMaxLength(50).IsRequired();

            _ = builder.HasIndex(os => os.OrderId).HasDatabaseName("IX_OrderStatus_OrderId");
            _ = builder.HasIndex(os => os.Status).HasDatabaseName("IX_OrderStatus_Status");
            _ = builder.HasIndex(os => os.Timestamp).HasDatabaseName("IX_OrderStatus_Timestamp");
        }
    }
}
