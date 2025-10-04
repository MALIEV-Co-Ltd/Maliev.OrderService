using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Maliev.OrderService.Data.Models;

namespace Maliev.OrderService.Data.Configurations;

public class ServiceCategoryConfiguration : IEntityTypeConfiguration<ServiceCategory>
{
    public void Configure(EntityTypeBuilder<ServiceCategory> builder)
    {
        builder.ToTable("service_categories");

        builder.HasKey(sc => sc.CategoryId);
        builder.Property(sc => sc.CategoryId).HasColumnName("category_id").ValueGeneratedOnAdd();
        builder.Property(sc => sc.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(sc => sc.Description).HasColumnName("description").HasColumnType("text");
        builder.Property(sc => sc.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        builder.HasIndex(sc => sc.Name).HasDatabaseName("IX_ServiceCategory_Name").IsUnique();
    }
}
