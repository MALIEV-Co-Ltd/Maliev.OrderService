using Microsoft.EntityFrameworkCore;
using Maliev.OrderService.Data.Models;
using Maliev.OrderService.Data.Configurations;

namespace Maliev.OrderService.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

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

        // Apply all configurations
        modelBuilder.ApplyConfiguration(new OrderConfiguration());
        modelBuilder.ApplyConfiguration(new OrderStatusConfiguration());
        modelBuilder.ApplyConfiguration(new OrderFileConfiguration());
        modelBuilder.ApplyConfiguration(new OrderNoteConfiguration());
        modelBuilder.ApplyConfiguration(new ServiceCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new ProcessTypeConfiguration());
        modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new NotificationSubscriptionConfiguration());
        modelBuilder.ApplyConfiguration(new Order3DPrintingAttributesConfiguration());
        modelBuilder.ApplyConfiguration(new OrderCncMachiningAttributesConfiguration());
        modelBuilder.ApplyConfiguration(new OrderSheetMetalAttributesConfiguration());
        modelBuilder.ApplyConfiguration(new Order3DScanningAttributesConfiguration());
        modelBuilder.ApplyConfiguration(new Order3DDesignAttributesConfiguration());
    }
}
