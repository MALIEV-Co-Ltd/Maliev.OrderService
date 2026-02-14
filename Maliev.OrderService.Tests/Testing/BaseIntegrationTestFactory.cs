using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Diagnostics.CodeAnalysis;
using MassTransit;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using Maliev.OrderService.Api.Services.External;
using Maliev.Aspire.ServiceDefaults.IAM;
using Moq;
using StackExchange.Redis;
using Xunit;
using Microsoft.AspNetCore.Builder;

// Disable parallel execution to prevent race conditions on the shared singleton database
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Maliev.OrderService.Tests.Testing
{
    /// <summary>
    /// Base integration test factory for OrderService.
    /// Provides PostgreSQL, Redis, and RabbitMQ containers with parallel startup.
    /// </summary>
    /// <typeparam name="TProgram">The Program class of the service being tested</typeparam>
    /// <typeparam name="TDbContext">The DbContext type for the service</typeparam>
    public class BaseIntegrationTestFactory<TProgram, TDbContext> : WebApplicationFactory<TProgram>, IAsyncLifetime
        where TProgram : class
        where TDbContext : DbContext
    {
        private static PostgreSqlContainer? _postgresContainer;
        private static RedisContainer? _redisContainer;
        private static RabbitMqContainer? _rabbitmqContainer;
        private static bool _containersStarted;
        private static readonly SemaphoreSlim _initLock = new(1, 1);

        private readonly RSA _testRsa;

        // Store permissions for test users
        private readonly ConcurrentDictionary<string, IEnumerable<string>> _userPermissions = new();

        /// <summary>
        /// Override this property if your DbContext connection string has a different name.
        /// Defaults to the DbContext class name.
        /// </summary>
        protected virtual string DbConnectionStringName => typeof(TDbContext).Name;

        public BaseIntegrationTestFactory()
        {
            _testRsa = RSA.Create(2048);

            // Set environment variable EARLY so Program.cs picks it up during WebApplication.CreateBuilder
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        }

        public async Task InitializeAsync()
        {
            await _initLock.WaitAsync();
            try
            {
                if (!_containersStarted)
                {
                    _postgresContainer = new PostgreSqlBuilder().WithName("postgres:18-alpine")
                        .Build();

                    _redisContainer = new RedisBuilder().WithName("redis:8.4-alpine")
                        .Build();

                    _rabbitmqContainer = new RabbitMqBuilder().WithName("rabbitmq:4.2-alpine")
                        .Build();

                    // Start all containers in parallel
                    await Task.WhenAll(
                        _postgresContainer.StartAsync(),
                        _redisContainer.StartAsync(),
                        _rabbitmqContainer.StartAsync()
                    );

                    // Ensure PostgreSQL is fully ready and accepting connections
                    var postgresReady = false;
                    var retryCount = 0;
                    const int maxRetries = 60;
                    while (!postgresReady && retryCount < maxRetries)
                    {
                        try
                        {
                            await using var conn = new Npgsql.NpgsqlConnection(_postgresContainer.GetConnectionString());
                            await conn.OpenAsync();
                            await using var cmd = conn.CreateCommand();
                            cmd.CommandText = "SELECT 1";
                            await cmd.ExecuteScalarAsync();
                            postgresReady = true;
                        }
                        catch
                        {
                            retryCount++;
                            await Task.Delay(1000);
                        }
                    }

                    if (!postgresReady)
                    {
                        throw new InvalidOperationException("PostgreSQL Testcontainer failed to become ready (Ping failed) after 60 seconds.");
                    }

                    // Wait for Redis to be ready
                    using (ConnectionMultiplexer connection = await ConnectionMultiplexer.ConnectAsync(_redisContainer.GetConnectionString()))
                    {
                        _ = await connection.GetDatabase().PingAsync();
                    }

                    // Apply database migrations
                    await ApplyMigrationsAsync();

                    _containersStarted = true;
                }
            }
            finally
            {
                _initLock.Release();
            }

            // Set environment variables immediately after containers start
            Environment.SetEnvironmentVariable($"ConnectionStrings__{DbConnectionStringName}", _postgresContainer!.GetConnectionString());
            Environment.SetEnvironmentVariable("ConnectionStrings__redis", _redisContainer!.GetConnectionString());
            Environment.SetEnvironmentVariable("ConnectionStrings__rabbitmq", _rabbitmqContainer!.GetConnectionString());
        }

        public new async Task DisposeAsync()
        {
            await base.DisposeAsync(); // Stop the Host
            // Static containers are NOT disposed here to allow reuse across tests
            _testRsa.Dispose();
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null); // Cleanup
        }


        protected override IHost CreateHost(IHostBuilder builder)
        {
            // Ensure containers are started before creating host
            if (!_containersStarted)
            {
                InitializeAsync().GetAwaiter().GetResult();
            }

            // Allow derived classes to set additional environment variables
            ConfigureEnvironmentVariables();

            return base.CreateHost(builder);
        }

        private string GetPublicKeyPemBase64()
        {
            // Export public key in PEM format
            string pemString = _testRsa.ExportSubjectPublicKeyInfoPem();
            // Base64 encode the PEM string as expected by ServiceDefaults
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(pemString));
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Set configuration directly into the WebHost configuration that Program.cs reads.
            _ = builder.UseSetting($"ConnectionStrings:{DbConnectionStringName}", _postgresContainer!.GetConnectionString());
            _ = builder.UseSetting("ConnectionStrings:redis", _redisContainer!.GetConnectionString());
            _ = builder.UseSetting("ConnectionStrings:rabbitmq", _rabbitmqContainer!.GetConnectionString());
            _ = builder.UseSetting("Features:PermissionBasedAuthEnabled", "true");
            _ = builder.UseSetting("IAM:RegistrationDelaySeconds", "0");
            _ = builder.UseSetting("Features:FailOpenOnIAMError", "true");

            _ = builder.ConfigureAppConfiguration((context, config) =>
            {
                _ = config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Service:Name"] = "OrderService",
                    ["Service:Version"] = "1.0.0-test",
                    ["Jwt:Issuer"] = "test-issuer",
                    ["Jwt:Audience"] = "test-audience",
                    ["Jwt:PublicKey"] = GetPublicKeyPemBase64(),
                    ["Jwt:SecurityKey"] = "test-secret-key-at-least-32-characters-long"
                });
            });

            _ = builder.ConfigureTestServices(services =>
            {
                // Configure JWT Bearer authentication with test RSA key
                _ = services.PostConfigureAll<JwtBearerOptions>(options =>
                {
                    // Disable claim type mapping to keep original claim names like "sub" instead of URIs
                    options.MapInboundClaims = false;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = "test-issuer",
                        ValidAudience = "test-audience",
                        IssuerSigningKey = new RsaSecurityKey(_testRsa),
                        ClockSkew = TimeSpan.Zero, // No clock skew for tests
                        NameClaimType = JwtRegisteredClaimNames.Sub,
                        RoleClaimType = "role"
                    };

                    // Add event to transform claims after token validation
                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = context =>
                        {
                            if (context.Principal?.Identity is ClaimsIdentity identity)
                            {
                                // Add ClaimTypes.NameIdentifier claim from "sub"
                                Claim? subClaim = identity.FindFirst(JwtRegisteredClaimNames.Sub);
                                if (subClaim != null && !identity.HasClaim(c => c.Type == ClaimTypes.NameIdentifier))
                                {
                                    identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, subClaim.Value));
                                }

                                // Add ClaimTypes.Role claims from "role"
                                var roleClaims = identity.FindAll("role").ToList();
                                foreach (Claim? roleClaim in roleClaims)
                                {
                                    if (!identity.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == roleClaim.Value))
                                    {
                                        identity.AddClaim(new Claim(ClaimTypes.Role, roleClaim.Value));
                                    }
                                }
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

                // Add MassTransit test harness for testing message publishing/consuming
                _ = services.AddMassTransitTestHarness();

                // Allow derived classes to add additional test services
                ConfigureAdditionalServices(services);

                // T010: Update factory to support permission-based token generation
                // Remove the real IAM client before adding the mock
                ServiceDescriptor? descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(Maliev.Aspire.ServiceDefaults.IAM.IIamServiceClient));
                if (descriptor != null)
                {
                    _ = services.Remove(descriptor);
                }

                // Mock IIamServiceClient to return permissions from our dictionary
                var mockIamClient = new Mock<Maliev.Aspire.ServiceDefaults.IAM.IIamServiceClient>();
                _ = mockIamClient.Setup(x => x.GetUserPermissionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((string userId, CancellationToken _) =>
                        _userPermissions.TryGetValue(userId, out IEnumerable<string>? perms) ? perms : []);

                _ = mockIamClient.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false); // Fallback to claims check in PermissionAuthorizationHandler

                _ = services.AddScoped(_ => mockIamClient.Object);
            });
        }

        /// <summary>
        /// Override this method to set additional environment variables before host creation.
        /// Called after standard environment variables are set.
        /// </summary>
        protected virtual void ConfigureEnvironmentVariables()
        {
            // Override in derived class if needed
        }

        /// <summary>
        /// Override this method to add additional test services to the DI container.
        /// </summary>
        protected virtual void ConfigureAdditionalServices(IServiceCollection services)
        {
            // Override in derived class if needed
        }

        /// <summary>
        /// Gets the DbContext from the service provider for use in tests.
        /// </summary>
        public TDbContext GetDbContext()
        {
            IServiceScope scope = Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<TDbContext>();
        }

        /// <summary>
        /// Creates a new DbContext instance for testing (not from DI container).
        /// </summary>
        public TDbContext CreateDbContext()
        {
            string connectionString = _postgresContainer!.GetConnectionString();
            var optionsBuilder = new DbContextOptionsBuilder<TDbContext>();
            _ = optionsBuilder.UseNpgsql(connectionString);
            return (TDbContext)Activator.CreateInstance(typeof(TDbContext), optionsBuilder.Options)!;
        }

        /// <summary>
        /// Applies all pending migrations to the test database.
        /// </summary>
        private async Task ApplyMigrationsAsync()
        {
            await using TDbContext context = CreateDbContext();
            await context.Database.MigrateAsync();
        }

        /// <summary>
        /// Cleans all data from the database while preserving schema.
        /// Queries the database schema dynamically to get all tables.
        /// </summary>
        [SuppressMessage("Security", "EF1002:Gaps in SQL queries", Justification = "Table names are retrieved from information_schema and are safe.")]
        public async Task CleanDatabaseAsync()
        {
            await using TDbContext context = CreateDbContext();

            // Get all table names from information_schema
            List<string> tableNames = await context.Database
                .SqlQueryRaw<string>(
                    @"SELECT table_name
                  FROM information_schema.tables
                  WHERE table_schema = 'public'
                  AND table_type = 'BASE TABLE'
                  AND table_name != '__EFMigrationsHistory'
                  ORDER BY table_name")
                .ToListAsync();

            // Truncate all tables (CASCADE handles foreign keys)
            foreach (string tableName in tableNames)
            {
                try
                {
                    _ = await context.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE \"{tableName}\" RESTART IDENTITY CASCADE");
                }
                catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P01")
                {
                    // Table doesn't exist - ignore this error
                }
            }
        }

        /// <summary>
        /// Alias for CleanDatabaseAsync to support different naming conventions.
        /// </summary>
        public Task ResetDatabaseAsync()
        {
            return CleanDatabaseAsync();
        }

        /// <summary>
        /// Alias for CleanDatabaseAsync to support different naming conventions.
        /// </summary>
        public Task ClearDatabaseAsync()
        {
            return CleanDatabaseAsync();
        }

        /// <summary>
        /// Clears the in-memory cache.
        /// </summary>
        public void ClearCache()
        {
            // Get IMemoryCache from services and cast to MemoryCache to access Clear()
            Microsoft.Extensions.Caching.Memory.IMemoryCache? memoryCache = Services.GetService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
            if (memoryCache is Microsoft.Extensions.Caching.Memory.MemoryCache cache)
            {
                cache.Compact(1.0); // Compact 100% removes all entries
            }
        }

        /// <summary>
        /// Exposes the RSA signing credentials for JWT token creation in tests.
        /// </summary>
        public SigningCredentials SigningCredentials => new(new RsaSecurityKey(_testRsa), SecurityAlgorithms.RsaSha256);

        /// <summary>
        /// Creates a test JWT token for authentication in integration tests.
        /// </summary>
        /// <param name="userId">User ID to include in token</param>
        /// <param name="roles">Roles to include in token claims</param>
        /// <param name="additionalClaims">Additional claims to include</param>
        /// <param name="permissions">Permissions to assign to this user via IIamServiceClient mock</param>
        /// <returns>JWT token string</returns>
        public string CreateTestJwtToken(
            string userId = "test-user",
            string[]? roles = null,
            Dictionary<string, string>? additionalClaims = null,
            IEnumerable<string>? permissions = null)
        {
            if (permissions != null)
            {
                _userPermissions[userId] = permissions;
            }

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userId),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            if (roles != null)
            {
                foreach (string role in roles)
                {
                    claims.Add(new Claim("role", role));
                }
            }

            if (permissions != null)
            {
                foreach (string permission in permissions)
                {
                    claims.Add(new Claim("permissions", permission));
                }
            }

            var rsaSecurityKey = new RsaSecurityKey(_testRsa);
            var signingCredentials = new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha256);

            var token = new JwtSecurityToken(
                issuer: "test-issuer",
                audience: "test-audience",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: signingCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Simplified JWT token generator with role parameter.
        /// Alias for CreateTestJwtToken to support different naming conventions.
        /// </summary>
        public string GenerateTestToken(string userId = "test-user", string role = "admin")
        {
            return CreateTestJwtToken(userId, [role]);
        }



        /// <summary>
        /// Creates an HTTP client with authenticated user and specified roles/permissions.
        /// </summary>
        public HttpClient CreateAuthenticatedClient(
            string userId = "test-user",
            string[]? roles = null,
            IEnumerable<string>? permissions = null)
        {
            string token = CreateTestJwtToken(userId, roles, permissions: permissions);
            HttpClient client = CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return client;
        }
    }
}
