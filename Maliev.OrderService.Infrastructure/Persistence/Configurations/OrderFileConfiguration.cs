using Maliev.OrderService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.OrderService.Infrastructure.Persistence.Configurations
{
    /// <summary>EF Core configuration for <see cref="OrderFile"/>.</summary>
    internal sealed class OrderFileConfiguration : IEntityTypeConfiguration<OrderFile>
    {
        public void Configure(EntityTypeBuilder<OrderFile> builder)
        {
            _ = builder.ToTable("order_files");

            _ = builder.HasKey(of => of.FileId);
            _ = builder.Property(of => of.FileId).HasColumnName("file_id").ValueGeneratedOnAdd();
            _ = builder.Property(of => of.OrderId).HasColumnName("order_id").HasMaxLength(50).IsRequired();
            _ = builder.Property(of => of.FileRole).HasColumnName("file_role").HasMaxLength(20).IsRequired();
            _ = builder.Property(of => of.FileCategory).HasColumnName("file_category").HasMaxLength(20).IsRequired();
            _ = builder.Property(of => of.DesignUnits).HasColumnName("design_units").HasMaxLength(10);
            _ = builder.Property(of => of.ObjectPath).HasColumnName("object_path").HasMaxLength(500).IsRequired();
            _ = builder.Property(of => of.FileName).HasColumnName("file_name").HasMaxLength(255).IsRequired();
            _ = builder.Property(of => of.FileSize).HasColumnName("file_size").IsRequired();
            _ = builder.Property(of => of.FileType).HasColumnName("file_type").HasMaxLength(50).IsRequired();
            _ = builder.Property(of => of.AccessLevel).HasColumnName("access_level").HasMaxLength(20).HasDefaultValue("Internal");
            _ = builder.Property(of => of.UploadedAt).HasColumnName("uploaded_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            _ = builder.Property(of => of.UploadedBy).HasColumnName("uploaded_by").HasMaxLength(50).IsRequired();
            _ = builder.Property(of => of.DeletedAt).HasColumnName("deleted_at");
            _ = builder.Property(of => of.IsPrimary).HasColumnName("is_primary").HasDefaultValue(false);

            _ = builder.HasIndex(of => of.OrderId).HasDatabaseName("IX_OrderFile_OrderId");
            _ = builder.HasIndex(of => of.FileRole).HasDatabaseName("IX_OrderFile_FileRole");
            _ = builder.HasIndex(of => of.FileCategory).HasDatabaseName("IX_OrderFile_FileCategory");
            _ = builder.HasIndex(of => of.ObjectPath).HasDatabaseName("IX_OrderFile_ObjectPath").IsUnique();
            _ = builder.HasIndex(of => of.DeletedAt).HasDatabaseName("IX_OrderFile_DeletedAt");
        }
    }
}
