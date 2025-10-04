using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Maliev.OrderService.Data.Models;

namespace Maliev.OrderService.Data.Configurations;

public class NotificationSubscriptionConfiguration : IEntityTypeConfiguration<NotificationSubscription>
{
    public void Configure(EntityTypeBuilder<NotificationSubscription> builder)
    {
        builder.ToTable("notification_subscriptions");

        builder.HasKey(ns => ns.SubscriptionId);
        builder.Property(ns => ns.SubscriptionId).HasColumnName("subscription_id").ValueGeneratedOnAdd();
        builder.Property(ns => ns.CustomerId).HasColumnName("customer_id").HasMaxLength(50).IsRequired();
        builder.Property(ns => ns.IsSubscribed).HasColumnName("is_subscribed").HasDefaultValue(true);
        builder.Property(ns => ns.Channels).HasColumnName("channels").HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(ns => ns.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(ns => ns.CustomerId).HasDatabaseName("IX_NotificationSubscription_CustomerId").IsUnique();
    }
}
