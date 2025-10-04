using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Maliev.OrderService.Data.Models;

namespace Maliev.OrderService.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(al => al.AuditId);
        builder.Property(al => al.AuditId).HasColumnName("audit_id").ValueGeneratedOnAdd();
        builder.Property(al => al.OrderId).HasColumnName("order_id").HasMaxLength(50).IsRequired();
        builder.Property(al => al.Action).HasColumnName("action").HasMaxLength(50).IsRequired();
        builder.Property(al => al.PerformedBy).HasColumnName("performed_by").HasMaxLength(50).IsRequired();
        builder.Property(al => al.PerformedAt).HasColumnName("performed_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(al => al.EntityType).HasColumnName("entity_type").HasMaxLength(50).IsRequired();
        builder.Property(al => al.EntityId).HasColumnName("entity_id").HasMaxLength(100).IsRequired();
        builder.Property(al => al.ChangeDetails).HasColumnName("change_details").HasColumnType("jsonb");

        builder.HasIndex(al => al.OrderId).HasDatabaseName("IX_AuditLog_OrderId");
        builder.HasIndex(al => al.PerformedBy).HasDatabaseName("IX_AuditLog_PerformedBy");
        builder.HasIndex(al => al.PerformedAt).HasDatabaseName("IX_AuditLog_PerformedAt");
        builder.HasIndex(al => al.Action).HasDatabaseName("IX_AuditLog_Action");
    }
}
