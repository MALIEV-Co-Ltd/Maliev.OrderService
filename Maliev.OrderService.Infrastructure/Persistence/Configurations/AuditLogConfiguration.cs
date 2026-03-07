using Maliev.OrderService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.OrderService.Infrastructure.Persistence.Configurations
{
    /// <summary>EF Core configuration for <see cref="AuditLog"/>.</summary>
    internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            _ = builder.ToTable("audit_logs");

            _ = builder.HasKey(al => al.AuditId);
            _ = builder.Property(al => al.AuditId).HasColumnName("audit_id").ValueGeneratedOnAdd();
            _ = builder.Property(al => al.OrderId).HasColumnName("order_id").HasMaxLength(50).IsRequired();
            _ = builder.Property(al => al.Action).HasColumnName("action").HasMaxLength(50).IsRequired();
            _ = builder.Property(al => al.PerformedBy).HasColumnName("performed_by").HasMaxLength(50).IsRequired();
            _ = builder.Property(al => al.PerformedAt).HasColumnName("performed_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            _ = builder.Property(al => al.EntityType).HasColumnName("entity_type").HasMaxLength(50).IsRequired();
            _ = builder.Property(al => al.EntityId).HasColumnName("entity_id").HasMaxLength(100).IsRequired();
            _ = builder.Property(al => al.ChangeDetails).HasColumnName("change_details").HasColumnType("jsonb");

            _ = builder.HasIndex(al => al.OrderId).HasDatabaseName("IX_AuditLog_OrderId");
            _ = builder.HasIndex(al => al.PerformedBy).HasDatabaseName("IX_AuditLog_PerformedBy");
            _ = builder.HasIndex(al => al.PerformedAt).HasDatabaseName("IX_AuditLog_PerformedAt");
            _ = builder.HasIndex(al => al.Action).HasDatabaseName("IX_AuditLog_Action");
        }
    }
}
