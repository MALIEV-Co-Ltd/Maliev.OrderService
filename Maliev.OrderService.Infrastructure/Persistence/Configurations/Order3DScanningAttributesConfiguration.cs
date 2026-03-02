using Maliev.OrderService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.OrderService.Infrastructure.Persistence.Configurations
{
    /// <summary>EF Core configuration for <see cref="Order3DScanningAttributes"/>.</summary>
    internal sealed class Order3DScanningAttributesConfiguration : IEntityTypeConfiguration<Order3DScanningAttributes>
    {
        public void Configure(EntityTypeBuilder<Order3DScanningAttributes> builder)
        {
            _ = builder.ToTable("order_3d_scanning_attributes");

            _ = builder.HasKey(attr => attr.OrderId);
            _ = builder.Property(attr => attr.OrderId).HasColumnName("order_id").HasMaxLength(50);
            _ = builder.Property(attr => attr.RequiredAccuracy).HasColumnName("required_accuracy").HasMaxLength(20);
            _ = builder.Property(attr => attr.ScanLocation).HasColumnName("scan_location").HasColumnType("text");
            _ = builder.Property(attr => attr.OutputFileFormats).HasColumnName("output_file_formats").HasMaxLength(100);
            _ = builder.Property(attr => attr.DeviationReportRequested).HasColumnName("deviation_report_requested").HasDefaultValue(false);
        }
    }
}
