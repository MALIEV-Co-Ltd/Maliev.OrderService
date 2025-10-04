using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Maliev.OrderService.Data.Models;

namespace Maliev.OrderService.Data.Configurations;

public class Order3DPrintingAttributesConfiguration : IEntityTypeConfiguration<Order3DPrintingAttributes>
{
    public void Configure(EntityTypeBuilder<Order3DPrintingAttributes> builder)
    {
        builder.ToTable("order_3d_printing_attributes");

        builder.HasKey(attr => attr.OrderId);
        builder.Property(attr => attr.OrderId).HasColumnName("order_id").HasMaxLength(50);
        builder.Property(attr => attr.ThreadTapRequired).HasColumnName("thread_tap_required").HasDefaultValue(false);
        builder.Property(attr => attr.InsertRequired).HasColumnName("insert_required").HasDefaultValue(false);
        builder.Property(attr => attr.PartMarking).HasColumnName("part_marking").HasMaxLength(100);
        builder.Property(attr => attr.PartAssemblyTestRequired).HasColumnName("part_assembly_test_required").HasDefaultValue(false);
    }
}
