using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Maliev.OrderService.Data.Models;

namespace Maliev.OrderService.Data.Configurations;

public class OrderFileConfiguration : IEntityTypeConfiguration<OrderFile>
{
    public void Configure(EntityTypeBuilder<OrderFile> builder)
    {
        builder.ToTable("order_files");

        builder.HasKey(of => of.FileId);
        builder.Property(of => of.FileId).HasColumnName("file_id").ValueGeneratedOnAdd();
        builder.Property(of => of.OrderId).HasColumnName("order_id").HasMaxLength(50).IsRequired();
        builder.Property(of => of.FileRole).HasColumnName("file_role").HasMaxLength(20).IsRequired();
        builder.Property(of => of.FileCategory).HasColumnName("file_category").HasMaxLength(20).IsRequired();
        builder.Property(of => of.DesignUnits).HasColumnName("design_units").HasMaxLength(10);
        builder.Property(of => of.ObjectPath).HasColumnName("object_path").HasMaxLength(500).IsRequired();
        builder.Property(of => of.FileName).HasColumnName("file_name").HasMaxLength(255).IsRequired();
        builder.Property(of => of.FileSize).HasColumnName("file_size").IsRequired();
        builder.Property(of => of.FileType).HasColumnName("file_type").HasMaxLength(50).IsRequired();
        builder.Property(of => of.AccessLevel).HasColumnName("access_level").HasMaxLength(20).HasDefaultValue("Internal");
        builder.Property(of => of.UploadedAt).HasColumnName("uploaded_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(of => of.UploadedBy).HasColumnName("uploaded_by").HasMaxLength(50).IsRequired();
        builder.Property(of => of.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(of => of.OrderId).HasDatabaseName("IX_OrderFile_OrderId");
        builder.HasIndex(of => of.FileRole).HasDatabaseName("IX_OrderFile_FileRole");
        builder.HasIndex(of => of.FileCategory).HasDatabaseName("IX_OrderFile_FileCategory");
        builder.HasIndex(of => of.ObjectPath).HasDatabaseName("IX_OrderFile_ObjectPath").IsUnique();
        builder.HasIndex(of => of.DeletedAt).HasDatabaseName("IX_OrderFile_DeletedAt");
    }
}
