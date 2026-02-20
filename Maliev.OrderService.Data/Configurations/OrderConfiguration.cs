using Maliev.OrderService.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.OrderService.Data.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            _ = builder.ToTable("orders");

            _ = builder.HasKey(o => o.OrderId);
            _ = builder.Property(o => o.OrderId).HasColumnName("order_id").HasMaxLength(50).IsRequired();

            _ = builder.Property(o => o.CustomerId).HasColumnName("customer_id").HasMaxLength(50).IsRequired();
            _ = builder.Property(o => o.CustomerType).HasColumnName("customer_type").HasMaxLength(20).IsRequired();
            _ = builder.Property(o => o.ServiceCategoryId).HasColumnName("service_category_id").IsRequired();
            _ = builder.Property(o => o.ProcessTypeId).HasColumnName("process_type_id");

            _ = builder.Property(o => o.MaterialId).HasColumnName("material_id");
            _ = builder.Property(o => o.ColorId).HasColumnName("color_id");
            _ = builder.Property(o => o.SurfaceFinishingId).HasColumnName("surface_finishing_id");
            _ = builder.Property(o => o.MaterialName).HasColumnName("material_name").HasMaxLength(100);
            _ = builder.Property(o => o.ColorName).HasColumnName("color_name").HasMaxLength(100);
            _ = builder.Property(o => o.SurfaceFinishingName).HasColumnName("surface_finishing_name").HasMaxLength(100);
            _ = builder.Property(o => o.MaterialCacheUpdatedAt).HasColumnName("material_cache_updated_at");

            _ = builder.Property(o => o.OrderedQuantity).HasColumnName("ordered_quantity");
            _ = builder.Property(o => o.ManufacturedQuantity).HasColumnName("manufactured_quantity").HasDefaultValue(0);

            _ = builder.Property(o => o.LeadTimeDays).HasColumnName("lead_time_days");
            _ = builder.Property(o => o.PromisedDeliveryDate).HasColumnName("promised_delivery_date");
            _ = builder.Property(o => o.ActualDeliveryDate).HasColumnName("actual_delivery_date");

            _ = builder.Property(o => o.QuotedAmount).HasColumnName("quoted_amount").HasColumnType("decimal(10,2)");
            _ = builder.Property(o => o.QuoteCurrency).HasColumnName("quote_currency").HasMaxLength(10).HasDefaultValue("THB");

            _ = builder.Property(o => o.IsConfidential).HasColumnName("is_confidential").HasDefaultValue(false);

            _ = builder.Property(o => o.PaymentId).HasColumnName("payment_id").HasMaxLength(50);
            _ = builder.Property(o => o.PaymentStatus).HasColumnName("payment_status").HasMaxLength(20).HasDefaultValue("Unpaid");

            _ = builder.Property(o => o.AssignedEmployeeId).HasColumnName("assigned_employee_id").HasMaxLength(50);
            _ = builder.Property(o => o.DepartmentId).HasColumnName("department_id").HasMaxLength(50);

            _ = builder.Property(o => o.Requirements).HasColumnName("requirements").HasColumnType("text");

            // Map xmin as a system shadow property for optimistic concurrency
            _ = builder.Property<uint>("xmin")
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            _ = builder.Property(o => o.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            _ = builder.Property(o => o.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            _ = builder.Property(o => o.CreatedBy).HasColumnName("created_by").HasMaxLength(50).IsRequired();
            _ = builder.Property(o => o.UpdatedBy).HasColumnName("updated_by").HasMaxLength(50).IsRequired();

            // Customer Purchase Order fields
            _ = builder.Property(o => o.CustomerPoNumber).HasColumnName("customer_po_number").HasMaxLength(50);
            _ = builder.Property(o => o.CustomerPoFileId).HasColumnName("customer_po_file_id");

            // Indexes
            _ = builder.HasIndex(o => o.CustomerId).HasDatabaseName("IX_Order_CustomerId");
            _ = builder.HasIndex(o => o.AssignedEmployeeId).HasDatabaseName("IX_Order_AssignedEmployeeId");
            _ = builder.HasIndex(o => o.DepartmentId).HasDatabaseName("IX_Order_DepartmentId");
            _ = builder.HasIndex(o => o.PaymentId).HasDatabaseName("IX_Order_PaymentId");
            _ = builder.HasIndex(o => o.MaterialId).HasDatabaseName("IX_Order_MaterialId");
            _ = builder.HasIndex(o => o.ProcessTypeId).HasDatabaseName("IX_Order_ProcessTypeId");
            _ = builder.HasIndex(o => o.CreatedAt).HasDatabaseName("IX_Order_CreatedAt");

            // Relationships
            _ = builder.HasOne(o => o.ServiceCategory)
                .WithMany(sc => sc.Orders)
                .HasForeignKey(o => o.ServiceCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            _ = builder.HasOne(o => o.ProcessType)
                .WithMany(pt => pt.Orders)
                .HasForeignKey(o => o.ProcessTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            _ = builder.HasMany(o => o.OrderStatuses)
                .WithOne(os => os.Order)
                .HasForeignKey(os => os.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            _ = builder.HasMany(o => o.OrderFiles)
                .WithOne(of => of.Order)
                .HasForeignKey(of => of.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            _ = builder.HasMany(o => o.OrderNotes)
                .WithOne(on => on.Order)
                .HasForeignKey(on => on.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            _ = builder.HasMany(o => o.AuditLogs)
                .WithOne(al => al.Order)
                .HasForeignKey(al => al.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            _ = builder.HasOne(o => o.PrintingAttributes)
                .WithOne(pa => pa.Order)
                .HasForeignKey<Order3DPrintingAttributes>(pa => pa.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            _ = builder.HasOne(o => o.CncAttributes)
                .WithOne(ca => ca.Order)
                .HasForeignKey<OrderCncMachiningAttributes>(ca => ca.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            _ = builder.HasOne(o => o.SheetMetalAttributes)
                .WithOne(sma => sma.Order)
                .HasForeignKey<OrderSheetMetalAttributes>(sma => sma.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            _ = builder.HasOne(o => o.ScanningAttributes)
                .WithOne(sa => sa.Order)
                .HasForeignKey<Order3DScanningAttributes>(sa => sa.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            _ = builder.HasOne(o => o.DesignAttributes)
                .WithOne(da => da.Order)
                .HasForeignKey<Order3DDesignAttributes>(da => da.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
