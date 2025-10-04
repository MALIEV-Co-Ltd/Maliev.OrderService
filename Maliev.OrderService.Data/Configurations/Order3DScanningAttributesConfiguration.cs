using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Maliev.OrderService.Data.Models;

namespace Maliev.OrderService.Data.Configurations;

public class Order3DScanningAttributesConfiguration : IEntityTypeConfiguration<Order3DScanningAttributes>
{
    public void Configure(EntityTypeBuilder<Order3DScanningAttributes> builder)
    {
        builder.ToTable("order_3d_scanning_attributes");

        builder.HasKey(attr => attr.OrderId);
        builder.Property(attr => attr.OrderId).HasColumnName("order_id").HasMaxLength(50);
        builder.Property(attr => attr.RequiredAccuracy).HasColumnName("required_accuracy").HasMaxLength(20);
        builder.Property(attr => attr.ScanLocation).HasColumnName("scan_location").HasColumnType("text");
        builder.Property(attr => attr.OutputFileFormats).HasColumnName("output_file_formats").HasMaxLength(100);
        builder.Property(attr => attr.DeviationReportRequested).HasColumnName("deviation_report_requested").HasDefaultValue(false);
    }
}
