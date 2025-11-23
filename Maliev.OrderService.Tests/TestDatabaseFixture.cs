using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Maliev.OrderService.Data;
using Maliev.OrderService.Data.Models;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Maliev.OrderService.Tests;

public partial class TestDatabaseFixture : IDisposable
{
    private readonly string _connectionString;

    public TestDatabaseFixture()
    {
        var configuration = BuildTestConfiguration();
        _connectionString = configuration.GetConnectionString("OrderDbContext")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:OrderDbContext not found. Configure via User Secrets, environment variable, or appsettings.Testing.json. " +
                "Example: Host=localhost;Port=5432;Database=test_db;Username=postgres;Password=your_password;");

        try
        {
            // Apply migrations to ensure database schema is up to date
            using var context = CreateDbContext();
            context.Database.Migrate();

            // Seed required reference data
            SeedReferenceData(context);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to connect to PostgreSQL for testing. Please ensure:\n" +
                $"1. PostgreSQL is running on the configured host/port\n" +
                $"2. The test_db database exists (or can be created)\n" +
                $"3. The connection string is correct\n" +
                $"Connection string (password hidden): {HidePassword(_connectionString)}\n" +
                $"Original error: {ex.Message}", ex);
        }
    }

    [GeneratedRegex(@"Password=[^;]+", RegexOptions.IgnoreCase)]
    private static partial Regex PasswordRegex();

    private static string HidePassword(string connectionString)
    {
        return PasswordRegex().Replace(connectionString, "Password=***");
    }

    public OrderDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

        return new OrderDbContext(options);
    }

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

    public void Cleanup()
    {
        // Clean up test data between tests (but keep reference data)
        using var context = CreateDbContext();

        // Remove all orders and related data (cascading delete handles related entities)
        context.Orders.RemoveRange(context.Orders);
        context.OrderStatuses.RemoveRange(context.OrderStatuses);
        context.OrderFiles.RemoveRange(context.OrderFiles);
        context.OrderNotes.RemoveRange(context.OrderNotes);

        context.SaveChanges();
    }

    public void Dispose()
    {
        // Optional: Clean up all test data on disposal
        Cleanup();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Builds IConfiguration for tests using the standard configuration hierarchy:
    /// appsettings.Testing.json -> User Secrets -> Environment Variables
    /// </summary>
    public static IConfiguration BuildTestConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Testing.json", optional: true)
            .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
            .AddEnvironmentVariables();

        return builder.Build();
    }
}
