using Maliev.OrderService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.OrderService.Infrastructure.Persistence.Configurations
{
    /// <summary>EF Core configuration for <see cref="OrderPreviewImage"/>.</summary>
    internal sealed class OrderPreviewImageConfiguration : IEntityTypeConfiguration<OrderPreviewImage>
    {
        public void Configure(EntityTypeBuilder<OrderPreviewImage> builder)
        {
            _ = builder.ToTable("order_preview_images");

            _ = builder.HasKey(opi => opi.PreviewImageId);
            _ = builder.Property(opi => opi.PreviewImageId).HasColumnName("preview_image_id").ValueGeneratedOnAdd();
            _ = builder.Property(opi => opi.OrderId).HasColumnName("order_id").HasMaxLength(50).IsRequired();
            _ = builder.Property(opi => opi.Side).HasColumnName("side").HasMaxLength(20).IsRequired();
            _ = builder.Property(opi => opi.StoragePath).HasColumnName("storage_path").HasMaxLength(500).IsRequired();
            _ = builder.Property(opi => opi.GeneratedAt).HasColumnName("generated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            _ = builder.Property(opi => opi.SourceFileId).HasColumnName("source_file_id");

            _ = builder.HasIndex(opi => opi.OrderId).HasDatabaseName("IX_OrderPreviewImage_OrderId");
            _ = builder.HasIndex(opi => opi.Side).HasDatabaseName("IX_OrderPreviewImage_Side");
            _ = builder.HasIndex(opi => new { opi.OrderId, opi.Side }).HasDatabaseName("IX_OrderPreviewImage_OrderId_Side").IsUnique();
        }
    }
}
