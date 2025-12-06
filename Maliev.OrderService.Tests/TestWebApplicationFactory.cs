using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Maliev.OrderService.Api.Services.External;
using Maliev.OrderService.Data;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Maliev.OrderService.Tests;

/// <summary>
/// Test web application factory for integration tests with Testcontainers
/// Uses dynamic RSA keys for JWT authentication testing
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly TestDatabaseFixture _dbFixture = new();
    private readonly RSA _testRsa;
    private const string TestIssuer = "test-issuer";
    private const string TestAudience = "test-audience";

    private static readonly string[] EmployeeRoles = ["Employee"];
    private static readonly string[] ManagerRoles = ["Manager"];
    private static readonly string[] AdminRoles = ["Admin"];
    private static readonly string[] CustomerRoles = ["Customer"];

    public TestWebApplicationFactory()
    {
        // Generate ephemeral RSA key for test JWT tokens
        _testRsa = RSA.Create(2048);
    }

    /// <summary>
    /// Initializes the test database fixture
    /// </summary>
    public async Task InitializeAsync()
    {
        await _dbFixture.InitializeAsync();
    }

    /// <summary>
    /// Disposes the test database fixture
    /// </summary>
    public new async Task DisposeAsync()
    {
        _testRsa.Dispose();
        await _dbFixture.DisposeAsync();
        await base.DisposeAsync();
    }

    /// <summary>
    /// Gets the connection string from the database fixture
    /// </summary>
    public string GetConnectionString() => _dbFixture.GetConnectionString();

    /// <summary>
    /// Creates a database context for testing
    /// </summary>
    public OrderDbContext CreateDbContext() => _dbFixture.CreateDbContext();

    /// <summary>
    /// Cleans up test data between tests
    /// </summary>
    public async Task CleanupAsync() => await _dbFixture.CleanupAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext configuration
            services.RemoveAll<DbContextOptions<OrderDbContext>>();

            // Use the Testcontainers connection string
            var connectionString = _dbFixture.GetConnectionString();

            services.AddDbContext<OrderDbContext>(options =>
            {
                options.UseNpgsql(connectionString);
            });

            // PostConfigure JWT Bearer options to use our test RSA key
            services.PostConfigureAll<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = TestIssuer,
                    ValidAudience = TestAudience,
                    IssuerSigningKey = new RsaSecurityKey(_testRsa),
                    ClockSkew = TimeSpan.Zero // No clock skew for tests
                };
            });

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

    /// <summary>
    /// Creates a test JWT token with specified claims for integration testing.
    /// </summary>
    /// <param name="userId">User ID claim</param>
    /// <param name="roles">User roles</param>
    /// <param name="additionalClaims">Additional claims to include</param>
    /// <returns>JWT token string</returns>
    public string CreateTestJwtToken(string userId = "test-user", string[]? roles = null, Dictionary<string, string>? additionalClaims = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("userType", "employee") // Default claim for order service authorization
        };

        // Add roles
        roles ??= new[] { "Admin" };
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Add additional claims
        if (additionalClaims != null)
        {
            foreach (var (key, value) in additionalClaims)
            {
                claims.Add(new Claim(key, value));
            }
        }

        var credentials = new SigningCredentials(
            new RsaSecurityKey(_testRsa),
            SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Creates HTTP client with JWT Bearer token authentication (Admin role by default)
    /// </summary>
    public HttpClient CreateAuthenticatedClient(string userId = "test-user", string[]? roles = null, Dictionary<string, string>? additionalClaims = null)
    {
        var client = CreateClient();
        var token = CreateTestJwtToken(userId, roles, additionalClaims);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Creates HTTP client with Employee role
    /// </summary>
    public HttpClient CreateEmployeeClient(string userId = "test-employee")
    {
        return CreateAuthenticatedClient(userId, EmployeeRoles, new Dictionary<string, string> { ["userType"] = "employee" });
    }

    /// <summary>
    /// Creates HTTP client with Manager role
    /// </summary>
    public HttpClient CreateManagerClient(string userId = "test-manager")
    {
        return CreateAuthenticatedClient(userId, ManagerRoles, new Dictionary<string, string> { ["userType"] = "employee" });
    }

    /// <summary>
    /// Creates HTTP client with Admin role
    /// </summary>
    public HttpClient CreateAdminClient(string userId = "test-admin")
    {
        return CreateAuthenticatedClient(userId, AdminRoles, new Dictionary<string, string> { ["userType"] = "employee" });
    }

    /// <summary>
    /// Creates HTTP client with Customer role
    /// </summary>
    public HttpClient CreateCustomerClient(string customerId = "test-customer")
    {
        return CreateAuthenticatedClient(customerId, CustomerRoles, new Dictionary<string, string> { ["userType"] = "customer" });
    }
}
