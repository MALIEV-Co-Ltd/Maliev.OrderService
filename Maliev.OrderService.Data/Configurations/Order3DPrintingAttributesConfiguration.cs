using Maliev.OrderService.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.OrderService.Data.Configurations
{
    public class Order3DPrintingAttributesConfiguration : IEntityTypeConfiguration<Order3DPrintingAttributes>
    {
        public void Configure(EntityTypeBuilder<Order3DPrintingAttributes> builder)
        {
            _ = builder.ToTable("order_3d_printing_attributes");

            _ = builder.HasKey(attr => attr.OrderId);
            _ = builder.Property(attr => attr.OrderId).HasColumnName("order_id").HasMaxLength(50);
            _ = builder.Property(attr => attr.ThreadTapRequired).HasColumnName("thread_tap_required").HasDefaultValue(false);
            _ = builder.Property(attr => attr.InsertRequired).HasColumnName("insert_required").HasDefaultValue(false);
            _ = builder.Property(attr => attr.PartMarking).HasColumnName("part_marking").HasMaxLength(100);
            _ = builder.Property(attr => attr.PartAssemblyTestRequired).HasColumnName("part_assembly_test_required").HasDefaultValue(false);
        }
    }
}
