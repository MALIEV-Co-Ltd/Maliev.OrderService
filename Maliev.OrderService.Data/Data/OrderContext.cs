using Microsoft.EntityFrameworkCore;
using Maliev.OrderService.Data.Models;

namespace Maliev.OrderService.Data.Data
{
    public class OrderContext : DbContext
    {
        public OrderContext(DbContextOptions<OrderContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Category { get; set; }
        public DbSet<FileFormat> FileFormat { get; set; }
        public DbSet<Order> Order { get; set; }
        public DbSet<OrderFile> OrderFile { get; set; }
        public DbSet<Process> Process { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");
                entity.Property(e => e.CreatedDate).HasColumnType("datetime").HasDefaultValueSql("(getutcdate())");
                entity.Property(e => e.ModifiedDate).HasColumnType("datetime").HasDefaultValueSql("(getutcdate())");
                entity.Property(e => e.Name).HasMaxLength(50);
            });

            modelBuilder.Entity<FileFormat>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");
                entity.Property(e => e.CreatedDate).HasColumnType("datetime").HasDefaultValueSql("(getutcdate())");
                entity.Property(e => e.Extension).HasMaxLength(50);
                entity.Property(e => e.ModifiedDate).HasColumnType("datetime").HasDefaultValueSql("(getutcdate())");
                entity.Property(e => e.Name).HasMaxLength(50);
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");
                entity.Property(e => e.ColorId).HasColumnName("ColorID");
                entity.Property(e => e.CreatedDate).HasColumnType("datetime").HasDefaultValueSql("(getutcdate())");
                entity.Property(e => e.CurrencyId).HasColumnName("CurrencyID");
                entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
                entity.Property(e => e.Description).HasMaxLength(250);
                entity.Property(e => e.DiscountPercent).HasColumnType("decimal(5, 2)");
                entity.Property(e => e.EmployeeId).HasColumnName("EmployeeID");
                entity.Property(e => e.FinishedDate).HasColumnType("date");
                entity.Property(e => e.MaterialId).HasColumnName("MaterialID");
                entity.Property(e => e.ModifiedDate).HasColumnType("datetime").HasDefaultValueSql("(getutcdate())");
                entity.Property(e => e.Name).HasMaxLength(100).HasDefaultValueSql("(N'unnamed')");
                entity.Property(e => e.ProcessId).HasColumnName("ProcessID");
                entity.Property(e => e.PromisedDate).HasColumnType("date");
                entity.Property(e => e.Remaining).HasComputedColumnSql("([Quantity]-[Manufactured])");
                entity.Property(e => e.Subtotal).HasColumnType("decimal(18, 2)").HasComputedColumnSql("(CONVERT([decimal](18,2),[UnitPrice]*[Quantity]-(([UnitPrice]*[Quantity])*[DiscountPercent])/(100)))");
                entity.Property(e => e.SurfaceFinishId).HasColumnName("SurfaceFinishID");
                entity.Property(e => e.TrackingNumber).HasMaxLength(250);
                entity.Property(e => e.Turnaround).HasComputedColumnSql("(datediff(day,[CreatedDate],[FinishedDate]))");
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

                entity.HasOne(d => d.Process)
                    .WithMany(p => p.Order)
                    .HasForeignKey(d => d.ProcessId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Order_Process");
            });

            modelBuilder.Entity<OrderFile>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");
                entity.Property(e => e.Bucket).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CreatedDate).HasColumnType("datetime").HasDefaultValueSql("(getutcdate())");
                entity.Property(e => e.ModifiedDate).HasColumnType("datetime").HasDefaultValueSql("(getutcdate())");
                entity.Property(e => e.ObjectName).IsRequired();
                entity.Property(e => e.OrderId).HasColumnName("OrderID");

                entity.HasOne(d => d.Order)
                    .WithMany(p => p.OrderFile)
                    .HasForeignKey(d => d.OrderId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_OrderFile_Order");
            });

            modelBuilder.Entity<Process>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");
                entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
                entity.Property(e => e.CreatedDate).HasColumnType("datetime").HasDefaultValueSql("(getutcdate())");
                entity.Property(e => e.ModifiedDate).HasColumnType("datetime").HasDefaultValueSql("(getutcdate())");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);

                entity.HasOne(d => d.Category)
                    .WithMany(p => p.Process)
                    .HasForeignKey(d => d.CategoryId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Process_Category");
            });
        }
    }
}
