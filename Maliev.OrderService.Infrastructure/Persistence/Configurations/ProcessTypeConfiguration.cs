using Maliev.OrderService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.OrderService.Infrastructure.Persistence.Configurations
{
    /// <summary>EF Core configuration for <see cref="ProcessType"/>.</summary>
    internal sealed class ProcessTypeConfiguration : IEntityTypeConfiguration<ProcessType>
    {
        public void Configure(EntityTypeBuilder<ProcessType> builder)
        {
            _ = builder.ToTable("process_types");

            _ = builder.HasKey(pt => pt.ProcessTypeId);
            _ = builder.Property(pt => pt.ProcessTypeId).HasColumnName("process_type_id").ValueGeneratedOnAdd();
            _ = builder.Property(pt => pt.ServiceCategoryId).HasColumnName("service_category_id").IsRequired();
            _ = builder.Property(pt => pt.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            _ = builder.Property(pt => pt.Description).HasColumnName("description").HasColumnType("text");
            _ = builder.Property(pt => pt.IsActive).HasColumnName("is_active").HasDefaultValue(true);

            _ = builder.HasIndex(pt => new { pt.ServiceCategoryId, pt.Name })
                .HasDatabaseName("IX_ProcessType_ServiceCategoryId_Name").IsUnique();
        }
    }
}
