using Maliev.OrderService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.OrderService.Infrastructure.Persistence.Configurations
{
    /// <summary>EF Core configuration for <see cref="OrderSheetMetalAttributes"/>.</summary>
    internal sealed class OrderSheetMetalAttributesConfiguration : IEntityTypeConfiguration<OrderSheetMetalAttributes>
    {
        public void Configure(EntityTypeBuilder<OrderSheetMetalAttributes> builder)
        {
            _ = builder.ToTable("order_sheet_metal_attributes");

            _ = builder.HasKey(attr => attr.OrderId);
            _ = builder.Property(attr => attr.OrderId).HasColumnName("order_id").HasMaxLength(50);
            _ = builder.Property(attr => attr.Thickness).HasColumnName("thickness").HasMaxLength(20);
            _ = builder.Property(attr => attr.WeldingRequired).HasColumnName("welding_required").HasDefaultValue(false);
            _ = builder.Property(attr => attr.WeldingDetails).HasColumnName("welding_details").HasColumnType("text");
            _ = builder.Property(attr => attr.Tolerance).HasColumnName("tolerance").HasMaxLength(50);
            _ = builder.Property(attr => attr.InspectionType).HasColumnName("inspection_type").HasMaxLength(50);
        }
    }
}
