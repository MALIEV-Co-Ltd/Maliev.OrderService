using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Maliev.OrderService.Data.Models;

namespace Maliev.OrderService.Data.Configurations;

public class OrderSheetMetalAttributesConfiguration : IEntityTypeConfiguration<OrderSheetMetalAttributes>
{
    public void Configure(EntityTypeBuilder<OrderSheetMetalAttributes> builder)
    {
        builder.ToTable("order_sheet_metal_attributes");

        builder.HasKey(attr => attr.OrderId);
        builder.Property(attr => attr.OrderId).HasColumnName("order_id").HasMaxLength(50);
        builder.Property(attr => attr.Thickness).HasColumnName("thickness").HasMaxLength(20);
        builder.Property(attr => attr.WeldingRequired).HasColumnName("welding_required").HasDefaultValue(false);
        builder.Property(attr => attr.WeldingDetails).HasColumnName("welding_details").HasColumnType("text");
        builder.Property(attr => attr.Tolerance).HasColumnName("tolerance").HasMaxLength(50);
        builder.Property(attr => attr.InspectionType).HasColumnName("inspection_type").HasMaxLength(50);
    }
}
