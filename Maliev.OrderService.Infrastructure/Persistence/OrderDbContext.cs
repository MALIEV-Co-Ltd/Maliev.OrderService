using Maliev.Aspire.ServiceDefaults.Database;
using Maliev.OrderService.Infrastructure.Persistence.Configurations;
using Maliev.OrderService.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Maliev.OrderService.Infrastructure.Persistence
{
    /// <summary>EF Core database context for the Order Service.</summary>
    /// <param name="options">The database context options.</param>
    public class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
    {
        /// <summary>Gets or sets the orders.</summary>
        public DbSet<Order> Orders { get; set; } = null!;
        /// <summary>Gets or sets the order statuses.</summary>
        public DbSet<OrderStatus> OrderStatuses { get; set; } = null!;
        /// <summary>Gets or sets the order files.</summary>
        public DbSet<OrderFile> OrderFiles { get; set; } = null!;
        /// <summary>Gets or sets the order notes.</summary>
        public DbSet<OrderNote> OrderNotes { get; set; } = null!;
        /// <summary>Gets or sets the service categories.</summary>
        public DbSet<ServiceCategory> ServiceCategories { get; set; } = null!;
        /// <summary>Gets or sets the process types.</summary>
        public DbSet<ProcessType> ProcessTypes { get; set; } = null!;
        /// <summary>Gets or sets the audit logs.</summary>
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        /// <summary>Gets or sets the notification subscriptions.</summary>
        public DbSet<NotificationSubscription> NotificationSubscriptions { get; set; } = null!;
        /// <summary>Gets or sets the 3D printing attributes.</summary>
        public DbSet<Order3DPrintingAttributes> Order3DPrintingAttributes { get; set; } = null!;
        /// <summary>Gets or sets the CNC machining attributes.</summary>
        public DbSet<OrderCncMachiningAttributes> OrderCncMachiningAttributes { get; set; } = null!;
        /// <summary>Gets or sets the sheet metal attributes.</summary>
        public DbSet<OrderSheetMetalAttributes> OrderSheetMetalAttributes { get; set; } = null!;
        /// <summary>Gets or sets the 3D scanning attributes.</summary>
        public DbSet<Order3DScanningAttributes> Order3DScanningAttributes { get; set; } = null!;
        /// <summary>Gets or sets the 3D design attributes.</summary>
        public DbSet<Order3DDesignAttributes> Order3DDesignAttributes { get; set; } = null!;
        /// <summary>Gets or sets the preview images.</summary>
        public DbSet<OrderPreviewImage> OrderPreviewImages { get; set; } = null!;

        /// <inheritdoc/>
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
            _ = modelBuilder.ApplyConfiguration(new OrderPreviewImageConfiguration());

            modelBuilder.AddInboxStateEntity();
            modelBuilder.AddOutboxMessageEntity();
            modelBuilder.AddOutboxStateEntity();

            // Apply PostgreSQL snake_case naming convention globally to all tables and columns
            SnakeCaseNamingHelper.ApplySnakeCaseNaming(modelBuilder);
        }
    }
}
