using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Maliev.OrderService.Api.Services.External;
using Maliev.OrderService.Data;
using Moq;
using System.Security.Claims;

namespace Maliev.OrderService.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext configuration
            services.RemoveAll<DbContextOptions<OrderDbContext>>();

            // Use actual PostgreSQL database for testing
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__OrderDbContext")
                ?? throw new InvalidOperationException(
                    "ConnectionStrings__OrderDbContext environment variable must be set for testing. " +
                    "Example: Host=localhost;Port=5432;Database=test_db;Username=postgres;Password=your_password;");

            services.AddDbContext<OrderDbContext>(options =>
            {
                options.UseNpgsql(connectionString);
            });

            // Mock authentication for testing
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

            // Mock external service clients
            MockExternalServices(services);

            // Build the service provider to initialize database
            var sp = services.BuildServiceProvider();

            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<OrderDbContext>();

            // Apply migrations to create database schema
            db.Database.Migrate();

            // Seed test data if needed
            SeedTestData(db);
        });

        builder.UseEnvironment("Testing");
    }

    private static void MockExternalServices(IServiceCollection services)
    {
        // Remove real HTTP client registrations
        services.RemoveAll<ICustomerServiceClient>();
        services.RemoveAll<IMaterialServiceClient>();
        services.RemoveAll<IPaymentServiceClient>();
        services.RemoveAll<IUploadServiceClient>();
        services.RemoveAll<IAuthServiceClient>();
        services.RemoveAll<IEmployeeServiceClient>();
        services.RemoveAll<INotificationServiceClient>();

        // Mock Customer Service
        var customerServiceMock = new Mock<ICustomerServiceClient>();
        customerServiceMock.Setup(x => x.HasActiveNdaAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        services.AddSingleton(customerServiceMock.Object);

        // Mock Material Service
        var materialServiceMock = new Mock<IMaterialServiceClient>();
        materialServiceMock.Setup(x => x.GetMaterialNameAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Test Material");
        services.AddSingleton(materialServiceMock.Object);

        // Mock Payment Service
        var paymentServiceMock = new Mock<IPaymentServiceClient>();
        services.AddSingleton(paymentServiceMock.Object);

        // Mock Upload Service
        var uploadServiceMock = new Mock<IUploadServiceClient>();
        uploadServiceMock.Setup(x => x.UploadFileAsync(
            It.IsAny<string>(),
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync((string path, Stream stream, string contentType, CancellationToken ct) =>
            new UploadFileResult
            {
                ObjectPath = path,
                FileSizeBytes = stream.CanSeek ? stream.Length : 1024,
                ContentType = contentType,
                UploadedAt = DateTime.UtcNow
            });

        uploadServiceMock.Setup(x => x.DownloadFileAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync((string path, CancellationToken ct) =>
            new MemoryStream(new byte[] { 1, 2, 3, 4, 5 }));

        uploadServiceMock.Setup(x => x.DeleteFileAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(true);

        services.AddSingleton(uploadServiceMock.Object);

        // Mock Auth Service
        var authServiceMock = new Mock<IAuthServiceClient>();
        services.AddSingleton(authServiceMock.Object);

        // Mock Employee Service
        var employeeServiceMock = new Mock<IEmployeeServiceClient>();
        services.AddSingleton(employeeServiceMock.Object);

        // Mock Notification Service
        var notificationServiceMock = new Mock<INotificationServiceClient>();
        services.AddSingleton(notificationServiceMock.Object);
    }

    private static void SeedTestData(OrderDbContext context)
    {
        // Seed service categories
        if (!context.ServiceCategories.Any())
        {
            context.ServiceCategories.AddRange(
                new Data.Models.ServiceCategory { CategoryId = 1, Name = "3D Printing", Description = "3D Printing services" },
                new Data.Models.ServiceCategory { CategoryId = 2, Name = "CNC Machining", Description = "CNC services" }
            );
        }

        // Seed process types
        if (!context.ProcessTypes.Any())
        {
            context.ProcessTypes.AddRange(
                new Data.Models.ProcessType { ProcessTypeId = 1, Name = "FDM", ServiceCategoryId = 1, Description = "Fused Deposition Modeling" },
                new Data.Models.ProcessType { ProcessTypeId = 7, Name = "Laser Cutting", ServiceCategoryId = 2, Description = "Laser cutting process" }
            );
        }

        context.SaveChanges();
    }
}

// Test authentication handler for integration tests
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        Microsoft.Extensions.Options.IOptionsMonitor<AuthenticationSchemeOptions> options,
        Microsoft.Extensions.Logging.ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Create claims for test user (Admin with all permissions)
        // Uses standard ASP.NET Core Identity claims
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim(ClaimTypes.Name, "test-user"),
            new Claim(ClaimTypes.Email, "test-user@example.com"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("userType", "employee")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
