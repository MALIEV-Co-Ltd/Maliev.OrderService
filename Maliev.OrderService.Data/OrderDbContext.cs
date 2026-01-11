using Microsoft.EntityFrameworkCore;
using Maliev.Aspire.ServiceDefaults.Database;
using Maliev.OrderService.Data.Models;
using Maliev.OrderService.Data.Configurations;

namespace Maliev.OrderService.Data
{
    public class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
    {

        // DbSets for all 13 entities
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderStatus> OrderStatuses { get; set; } = null!;
        public DbSet<OrderFile> OrderFiles { get; set; } = null!;
        public DbSet<OrderNote> OrderNotes { get; set; } = null!;
        public DbSet<ServiceCategory> ServiceCategories { get; set; } = null!;
        public DbSet<ProcessType> ProcessTypes { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<NotificationSubscription> NotificationSubscriptions { get; set; } = null!;
        public DbSet<Order3DPrintingAttributes> Order3DPrintingAttributes { get; set; } = null!;
        public DbSet<OrderCncMachiningAttributes> OrderCncMachiningAttributes { get; set; } = null!;
        public DbSet<OrderSheetMetalAttributes> OrderSheetMetalAttributes { get; set; } = null!;
        public DbSet<Order3DScanningAttributes> Order3DScanningAttributes { get; set; } = null!;
        public DbSet<Order3DDesignAttributes> Order3DDesignAttributes { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Define sequence for OrderId generation
            _ = modelBuilder.HasSequence<long>("order_id_seq")
                .StartsAt(1)
                .IncrementsBy(1);

            // Apply all configurations
            _ = modelBuilder.ApplyConfiguration(new OrderConfiguration());
            _ = modelBuilder.ApplyConfiguration(new OrderStatusConfiguration());
            _ = modelBuilder.ApplyConfiguration(new OrderFileConfiguration());
            _ = modelBuilder.ApplyConfiguration(new OrderNoteConfiguration());
            _ = modelBuilder.ApplyConfiguration(new ServiceCategoryConfiguration());
            _ = modelBuilder.ApplyConfiguration(new ProcessTypeConfiguration());
            _ = modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
            _ = modelBuilder.ApplyConfiguration(new NotificationSubscriptionConfiguration());
            _ = modelBuilder.ApplyConfiguration(new Order3DPrintingAttributesConfiguration());
            _ = modelBuilder.ApplyConfiguration(new OrderCncMachiningAttributesConfiguration());
            _ = modelBuilder.ApplyConfiguration(new OrderSheetMetalAttributesConfiguration());
            _ = modelBuilder.ApplyConfiguration(new Order3DScanningAttributesConfiguration());
            _ = modelBuilder.ApplyConfiguration(new Order3DDesignAttributesConfiguration());

            // Apply PostgreSQL snake_case naming convention globally to all tables and columns
            SnakeCaseNamingHelper.ApplySnakeCaseNaming(modelBuilder);
        }
    }
}
