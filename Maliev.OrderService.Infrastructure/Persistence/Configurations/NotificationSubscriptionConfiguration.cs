using Maliev.OrderService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.OrderService.Infrastructure.Persistence.Configurations
{
    /// <summary>EF Core configuration for <see cref="NotificationSubscription"/>.</summary>
    internal sealed class NotificationSubscriptionConfiguration : IEntityTypeConfiguration<NotificationSubscription>
    {
        public void Configure(EntityTypeBuilder<NotificationSubscription> builder)
        {
            _ = builder.ToTable("notification_subscriptions");

            _ = builder.HasKey(ns => ns.SubscriptionId);
            _ = builder.Property(ns => ns.SubscriptionId).HasColumnName("subscription_id").ValueGeneratedOnAdd();
            _ = builder.Property(ns => ns.CustomerId).HasColumnName("customer_id").HasMaxLength(50).IsRequired();
            _ = builder.Property(ns => ns.IsSubscribed).HasColumnName("is_subscribed").HasDefaultValue(true);
            _ = builder.Property(ns => ns.Channels).HasColumnName("channels").HasColumnType("jsonb").HasDefaultValue("[]");
            _ = builder.Property(ns => ns.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            _ = builder.HasIndex(ns => ns.CustomerId).HasDatabaseName("IX_NotificationSubscription_CustomerId").IsUnique();
        }
    }
}
