using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Maliev.OrderService.Data.Models;

namespace Maliev.OrderService.Data.Configurations;

public class ProcessTypeConfiguration : IEntityTypeConfiguration<ProcessType>
{
    public void Configure(EntityTypeBuilder<ProcessType> builder)
    {
        builder.ToTable("process_types");

        builder.HasKey(pt => pt.ProcessTypeId);
        builder.Property(pt => pt.ProcessTypeId).HasColumnName("process_type_id").ValueGeneratedOnAdd();
        builder.Property(pt => pt.ServiceCategoryId).HasColumnName("service_category_id").IsRequired();
        builder.Property(pt => pt.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(pt => pt.Description).HasColumnName("description").HasColumnType("text");
        builder.Property(pt => pt.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        builder.HasIndex(pt => new { pt.ServiceCategoryId, pt.Name })
            .HasDatabaseName("IX_ProcessType_ServiceCategoryId_Name").IsUnique();
    }
}
