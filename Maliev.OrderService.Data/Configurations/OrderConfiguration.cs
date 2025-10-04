using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Maliev.OrderService.Data.Models;

namespace Maliev.OrderService.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => o.OrderId);
        builder.Property(o => o.OrderId).HasColumnName("order_id").HasMaxLength(50).IsRequired();

        builder.Property(o => o.CustomerId).HasColumnName("customer_id").HasMaxLength(50).IsRequired();
        builder.Property(o => o.CustomerType).HasColumnName("customer_type").HasMaxLength(20).IsRequired();
        builder.Property(o => o.ServiceCategoryId).HasColumnName("service_category_id").IsRequired();
        builder.Property(o => o.ProcessTypeId).HasColumnName("process_type_id");

        builder.Property(o => o.MaterialId).HasColumnName("material_id");
        builder.Property(o => o.ColorId).HasColumnName("color_id");
        builder.Property(o => o.SurfaceFinishingId).HasColumnName("surface_finishing_id");
        builder.Property(o => o.MaterialName).HasColumnName("material_name").HasMaxLength(100);
        builder.Property(o => o.ColorName).HasColumnName("color_name").HasMaxLength(100);
        builder.Property(o => o.SurfaceFinishingName).HasColumnName("surface_finishing_name").HasMaxLength(100);
        builder.Property(o => o.MaterialCacheUpdatedAt).HasColumnName("material_cache_updated_at");

        builder.Property(o => o.OrderedQuantity).HasColumnName("ordered_quantity");
        builder.Property(o => o.ManufacturedQuantity).HasColumnName("manufactured_quantity").HasDefaultValue(0);

        builder.Property(o => o.LeadTimeDays).HasColumnName("lead_time_days");
        builder.Property(o => o.PromisedDeliveryDate).HasColumnName("promised_delivery_date");
        builder.Property(o => o.ActualDeliveryDate).HasColumnName("actual_delivery_date");

        builder.Property(o => o.QuotedAmount).HasColumnName("quoted_amount").HasColumnType("decimal(10,2)");
        builder.Property(o => o.QuoteCurrency).HasColumnName("quote_currency").HasMaxLength(10).HasDefaultValue("THB");

        builder.Property(o => o.IsConfidential).HasColumnName("is_confidential").HasDefaultValue(false);

        builder.Property(o => o.PaymentId).HasColumnName("payment_id").HasMaxLength(50);
        builder.Property(o => o.PaymentStatus).HasColumnName("payment_status").HasMaxLength(20).HasDefaultValue("Unpaid");

        builder.Property(o => o.AssignedEmployeeId).HasColumnName("assigned_employee_id").HasMaxLength(50);
        builder.Property(o => o.DepartmentId).HasColumnName("department_id").HasMaxLength(50);

        builder.Property(o => o.Requirements).HasColumnName("requirements").HasColumnType("text");

        builder.Property(o => o.Version).HasColumnName("version").IsRowVersion().IsRequired();
        builder.Property(o => o.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(o => o.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(o => o.CreatedBy).HasColumnName("created_by").HasMaxLength(50).IsRequired();
        builder.Property(o => o.UpdatedBy).HasColumnName("updated_by").HasMaxLength(50).IsRequired();

        // Indexes
        builder.HasIndex(o => o.CustomerId).HasDatabaseName("IX_Order_CustomerId");
        builder.HasIndex(o => o.AssignedEmployeeId).HasDatabaseName("IX_Order_AssignedEmployeeId");
        builder.HasIndex(o => o.DepartmentId).HasDatabaseName("IX_Order_DepartmentId");
        builder.HasIndex(o => o.PaymentId).HasDatabaseName("IX_Order_PaymentId");
        builder.HasIndex(o => o.MaterialId).HasDatabaseName("IX_Order_MaterialId");
        builder.HasIndex(o => o.ProcessTypeId).HasDatabaseName("IX_Order_ProcessTypeId");
        builder.HasIndex(o => o.CreatedAt).HasDatabaseName("IX_Order_CreatedAt");

        // Relationships
        builder.HasOne(o => o.ServiceCategory)
            .WithMany(sc => sc.Orders)
            .HasForeignKey(o => o.ServiceCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.ProcessType)
            .WithMany(pt => pt.Orders)
            .HasForeignKey(o => o.ProcessTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.OrderStatuses)
            .WithOne(os => os.Order)
            .HasForeignKey(os => os.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.OrderFiles)
            .WithOne(of => of.Order)
            .HasForeignKey(of => of.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.OrderNotes)
            .WithOne(on => on.Order)
            .HasForeignKey(on => on.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.AuditLogs)
            .WithOne(al => al.Order)
            .HasForeignKey(al => al.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.PrintingAttributes)
            .WithOne(pa => pa.Order)
            .HasForeignKey<Order3DPrintingAttributes>(pa => pa.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.CncAttributes)
            .WithOne(ca => ca.Order)
            .HasForeignKey<OrderCncMachiningAttributes>(ca => ca.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.SheetMetalAttributes)
            .WithOne(sma => sma.Order)
            .HasForeignKey<OrderSheetMetalAttributes>(sma => sma.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.ScanningAttributes)
            .WithOne(sa => sa.Order)
            .HasForeignKey<Order3DScanningAttributes>(sa => sa.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.DesignAttributes)
            .WithOne(da => da.Order)
            .HasForeignKey<Order3DDesignAttributes>(da => da.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
