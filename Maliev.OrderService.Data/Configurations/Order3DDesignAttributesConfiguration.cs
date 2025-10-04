using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Maliev.OrderService.Data.Models;

namespace Maliev.OrderService.Data.Configurations;

public class Order3DDesignAttributesConfiguration : IEntityTypeConfiguration<Order3DDesignAttributes>
{
    public void Configure(EntityTypeBuilder<Order3DDesignAttributes> builder)
    {
        builder.ToTable("order_3d_design_attributes");

        builder.HasKey(attr => attr.OrderId);
        builder.Property(attr => attr.OrderId).HasColumnName("order_id").HasMaxLength(50);
        builder.Property(attr => attr.ComplexityLevel).HasColumnName("complexity_level").HasMaxLength(20);
        builder.Property(attr => attr.Deliverables).HasColumnName("deliverables").HasMaxLength(200);
        builder.Property(attr => attr.DesignSoftware).HasColumnName("design_software").HasMaxLength(50);
        builder.Property(attr => attr.RevisionRounds).HasColumnName("revision_rounds").HasDefaultValue(2);
    }
}
