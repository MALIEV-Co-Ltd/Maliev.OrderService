using Maliev.OrderService.Domain.Entities;
using Maliev.OrderService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Maliev.OrderService.Tests.Infrastructure;

/// <summary>
/// Verifies the preserved migration history uses PostgreSQL's native xmin system column safely.
/// </summary>
[Collection("Database")]
public sealed class PostgreSqlMigrationSafetyTests(TestWebApplicationFactory factory)
{
    /// <summary>
    /// Verifies migrations expose only PostgreSQL's negative-attnum system xmin column.
    /// </summary>
    [Fact]
    public async Task AppliedMigrationsUseNativeSystemXmin()
    {
        await using OrderDbContext context = factory.CreateDbContext();
        await context.Database.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            "SELECT attnum FROM pg_attribute WHERE attrelid = 'orders'::regclass AND attname = 'xmin' AND NOT attisdropped",
            (NpgsqlConnection)context.Database.GetDbConnection());

        object? result = await command.ExecuteScalarAsync();

        short attributeNumber = Assert.IsType<short>(result);
        Assert.True(attributeNumber < 0, "xmin must be PostgreSQL's system column, not an application-created column.");
    }

    /// <summary>
    /// Verifies stale order writes fail through the native xmin concurrency token.
    /// </summary>
    [Fact]
    public async Task ConcurrentOrderUpdateThrowsForStaleXmin()
    {
        string orderId = $"MIGRATION-{Guid.NewGuid():N}";
        await using (OrderDbContext createContext = factory.CreateDbContext())
        {
            if (!await createContext.ServiceCategories.AnyAsync(category => category.CategoryId == 1))
            {
                createContext.ServiceCategories.Add(new ServiceCategory
                {
                    CategoryId = 1,
                    Name = "Migration safety test",
                    IsActive = true
                });
            }

            createContext.Orders.Add(new Order
            {
                OrderId = orderId,
                CustomerId = "migration-test",
                CustomerType = "Customer",
                ServiceCategoryId = 1,
                CreatedBy = "migration-test",
                UpdatedBy = "migration-test"
            });
            await createContext.SaveChangesAsync();
        }

        await using OrderDbContext firstContext = factory.CreateDbContext();
        await using OrderDbContext staleContext = factory.CreateDbContext();
        Order first = await firstContext.Orders.SingleAsync(order => order.OrderId == orderId);
        Order stale = await staleContext.Orders.SingleAsync(order => order.OrderId == orderId);

        first.Requirements = "first update";
        first.UpdatedAt = DateTime.UtcNow;
        await firstContext.SaveChangesAsync();

        stale.Requirements = "stale update";
        stale.UpdatedAt = DateTime.UtcNow.AddSeconds(1);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => staleContext.SaveChangesAsync());
    }
}
