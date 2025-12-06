using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Maliev.OrderService.Data;
using Maliev.OrderService.Data.Models;
using System.Reflection;
using Testcontainers.PostgreSql;

namespace Maliev.OrderService.Tests;

/// <summary>
/// Test database fixture using Testcontainers for isolated PostgreSQL testing
/// </summary>
public class TestDatabaseFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgresContainer;
    private string _connectionString = string.Empty;

    /// <summary>
    /// Initializes the test database container
    /// </summary>
    public async Task InitializeAsync()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:18")
            .WithDatabase("order_test_db")
            .WithUsername("postgres")
            .WithPassword("test_password")
            .Build();

        await _postgresContainer.StartAsync();
        _connectionString = _postgresContainer.GetConnectionString();

        // Apply migrations to create database schema
        using var context = CreateDbContext();
        await context.Database.MigrateAsync();

        // Seed required reference data
        SeedReferenceData(context);
    }

    /// <summary>
    /// Disposes the test database container
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_postgresContainer != null)
        {
            await _postgresContainer.DisposeAsync();
        }
    }

    /// <summary>
    /// Creates a new database context connected to the test container
    /// </summary>
    public OrderDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

        return new OrderDbContext(options);
    }

    /// <summary>
    /// Gets the connection string for the test database
    /// </summary>
    public string GetConnectionString() => _connectionString;

    /// <summary>
    /// Seeds required reference data for tests
    /// </summary>
    private static void SeedReferenceData(OrderDbContext context)
    {
        // Seed service categories if they don't exist
        if (!context.ServiceCategories.Any(sc => sc.CategoryId == 1))
        {
            context.ServiceCategories.AddRange(
                new ServiceCategory { CategoryId = 1, Name = "3D Printing", Description = "3D Printing services", IsActive = true },
                new ServiceCategory { CategoryId = 2, Name = "CNC Machining", Description = "CNC services", IsActive = true }
            );
        }

        // Seed process types if they don't exist
        if (!context.ProcessTypes.Any(pt => pt.ProcessTypeId == 1))
        {
            context.ProcessTypes.AddRange(
                new ProcessType { ProcessTypeId = 1, Name = "FDM", ServiceCategoryId = 1, Description = "Fused Deposition Modeling", IsActive = true },
                new ProcessType { ProcessTypeId = 2, Name = "SLA", ServiceCategoryId = 1, Description = "Stereolithography", IsActive = true },
                new ProcessType { ProcessTypeId = 7, Name = "Laser Cutting", ServiceCategoryId = 2, Description = "Laser cutting process", IsActive = true }
            );
        }

        context.SaveChanges();
    }

    /// <summary>
    /// Cleans up test data between tests (but keeps reference data)
    /// </summary>
    public async Task CleanupAsync()
    {
        // Force garbage collection and clear connection pools
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        for (int i = 0; i < 3; i++)
        {
            Npgsql.NpgsqlConnection.ClearAllPools();
            await Task.Delay(200);
        }

        await using var context = CreateDbContext();

        // Terminate any active connections
        await context.Database.ExecuteSqlRawAsync(@"
            SELECT pg_terminate_backend(pid)
            FROM pg_stat_activity
            WHERE datname = current_database()
            AND pid <> pg_backend_pid()
            AND state IN ('idle in transaction', 'active');
        ");

        await Task.Delay(100);

        // Truncate tables (keeping reference data)
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE order_files CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE order_notes CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE order_statuses CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE orders CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE audit_logs CASCADE");

        context.ChangeTracker.Clear();
        await context.Database.CloseConnectionAsync();

        Npgsql.NpgsqlConnection.ClearAllPools();
    }

    /// <summary>
    /// Legacy method for compatibility - cleanup synchronously
    /// </summary>
    public void Cleanup()
    {
        CleanupAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Builds IConfiguration for tests (for backward compatibility)
    /// Returns a configuration that uses the Testcontainers connection string
    /// </summary>
    public static IConfiguration BuildTestConfiguration()
    {
        // This method is kept for backward compatibility but is not used
        // when Testcontainers is active
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Testing.json", optional: true)
            .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
            .AddEnvironmentVariables();

        return builder.Build();
    }
}
