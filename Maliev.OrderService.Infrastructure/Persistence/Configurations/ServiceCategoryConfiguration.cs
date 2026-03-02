using Maliev.OrderService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.OrderService.Infrastructure.Persistence.Configurations
{
    /// <summary>EF Core configuration for <see cref="ServiceCategory"/>.</summary>
    internal sealed class ServiceCategoryConfiguration : IEntityTypeConfiguration<ServiceCategory>
    {
        public void Configure(EntityTypeBuilder<ServiceCategory> builder)
        {
            _ = builder.ToTable("service_categories");

            _ = builder.HasKey(sc => sc.CategoryId);
            _ = builder.Property(sc => sc.CategoryId).HasColumnName("category_id").ValueGeneratedOnAdd();
            _ = builder.Property(sc => sc.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            _ = builder.Property(sc => sc.Description).HasColumnName("description").HasColumnType("text");
            _ = builder.Property(sc => sc.IsActive).HasColumnName("is_active").HasDefaultValue(true);

            _ = builder.HasIndex(sc => sc.Name).HasDatabaseName("IX_ServiceCategory_Name").IsUnique();
        }
    }
}
