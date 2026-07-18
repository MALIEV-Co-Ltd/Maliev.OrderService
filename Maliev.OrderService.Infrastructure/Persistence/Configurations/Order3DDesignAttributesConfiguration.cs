using Maliev.OrderService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.OrderService.Infrastructure.Persistence.Configurations
{
    /// <summary>EF Core configuration for <see cref="Order3DDesignAttributes"/>.</summary>
    internal sealed class Order3DDesignAttributesConfiguration : IEntityTypeConfiguration<Order3DDesignAttributes>
    {
        public void Configure(EntityTypeBuilder<Order3DDesignAttributes> builder)
        {
            _ = builder.ToTable("order_3d_design_attributes");

            _ = builder.HasKey(attr => attr.OrderId);
            _ = builder.Property(attr => attr.OrderId).HasColumnName("order_id").HasMaxLength(50);
            _ = builder.Property(attr => attr.ComplexityLevel).HasColumnName("complexity_level").HasMaxLength(20);
            _ = builder.Property(attr => attr.Deliverables).HasColumnName("deliverables").HasMaxLength(200);
            _ = builder.Property(attr => attr.DesignSoftware).HasColumnName("design_software").HasMaxLength(50);
            _ = builder.Property(attr => attr.RevisionRounds).HasColumnName("revision_rounds").HasDefaultValue(2);
        }
    }
}
