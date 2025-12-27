using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Maliev.OrderService.Data.Models;

namespace Maliev.OrderService.Data.Configurations
{
    public class OrderCncMachiningAttributesConfiguration : IEntityTypeConfiguration<OrderCncMachiningAttributes>
    {
        public void Configure(EntityTypeBuilder<OrderCncMachiningAttributes> builder)
        {
            _ = builder.ToTable("order_cnc_machining_attributes");

            _ = builder.HasKey(attr => attr.OrderId);
            _ = builder.Property(attr => attr.OrderId).HasColumnName("order_id").HasMaxLength(50);
            _ = builder.Property(attr => attr.TapRequired).HasColumnName("tap_required").HasDefaultValue(false);
            _ = builder.Property(attr => attr.Tolerance).HasColumnName("tolerance").HasMaxLength(50);
            _ = builder.Property(attr => attr.SurfaceRoughness).HasColumnName("surface_roughness").HasMaxLength(20);
            _ = builder.Property(attr => attr.InspectionType).HasColumnName("inspection_type").HasMaxLength(50);

            _ = builder.HasIndex(attr => attr.Tolerance).HasDatabaseName("IX_OrderCncMachiningAttributes_Tolerance");
        }
    }
}
