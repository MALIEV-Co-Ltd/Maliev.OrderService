using Maliev.Aspire.ServiceDefaults;
using Maliev.OrderService.Api.Consumers;
using Maliev.OrderService.Api.Extensions;
using Maliev.OrderService.Api.Services;
using Maliev.OrderService.Api.Services.Business;
using Maliev.OrderService.Api.Services.External;
using Maliev.OrderService.Data;
using System.Threading.RateLimiting;

// Initialize bootstrap logging
using ILoggerFactory loggerFactory = LoggerFactory.Create(logBuilder => logBuilder.AddConsole());
ILogger bootstrapLogger = loggerFactory.CreateLogger("Program");

try
{
    Log.StartingHost(bootstrapLogger, "Order Service");

    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    // --- Secrets & Configuration ---
    _ = builder.AddGoogleSecretManagerVolume(); // Load secrets from /mnt/secrets if available

    // --- Infrastructure & Observability ---
    _ = builder.AddServiceDefaults(); // OpenTelemetry, health checks, resilience
    _ = builder.AddStandardMiddleware(options =>
    {
        options.EnableRequestLogging = true;
    });
    _ = builder.AddServiceMeters("orders-meter"); // Register service meters for OpenTelemetry business metrics

    _ = builder.AddRedisDistributedCache(instanceName: "order:"); // Redis with in-memory fallback
    _ = builder.AddMassTransitWithRabbitMq(cfg =>
    {
        _ = cfg.AddConsumer<PaymentCompletedEventConsumer>();
        _ = cfg.AddConsumer<FileDeletedEventConsumer>();
    }); // RabbitMQ message bus with consumers
    _ = builder.AddPostgresDbContext<OrderDbContext>(connectionName: "OrderDbContext"); // PostgreSQL with retry logic

    // --- API Configuration ---
    _ = builder.AddDefaultCors(); // CORS from CORS:AllowedOrigins config
    _ = builder.AddDefaultApiVersioning(); // API versioning with URL segment reader

    // JWT Authentication (tests override via PostConfigureAll with dynamic RSA keys)
    _ = builder.AddJwtAuthentication();
    _ = builder.Services.AddPermissionAuthorization();

    // Add specific policies
    _ = builder.Services.AddAuthorizationBuilder()
        .AddPolicy("Admin", policy =>
        {
            _ = policy.RequireAuthenticatedUser();
            _ = policy.RequireClaim("role", "admin");
        });

    // Add OpenAPI (must be in Program.cs for XML comments to work via source generator)
    if (!builder.Environment.IsProduction())
    {
        _ = builder.AddStandardOpenApi(
            title: "MALIEV Order Service API",
            description: "Sales order processing service. Manages order lifecycle from creation to fulfillment, batch order operations, status history tracking, file attachments, internal notes, and cancellation with reason tracking.");
    }

    _ = builder.Services.AddControllers();
    // External Service HttpClients with Standard Resilience Handler
    _ = builder.AddServiceClient<ICustomerServiceClient, CustomerServiceClient>("CustomerService");
    _ = builder.AddServiceClient<IMaterialServiceClient, MaterialServiceClient>("MaterialService");
    _ = builder.AddServiceClient<IPaymentServiceClient, PaymentServiceClient>("PaymentService");
    _ = builder.AddServiceClient<IUploadServiceClient, UploadServiceClient>("UploadService");
    _ = builder.AddServiceClient<IAuthServiceClient, AuthServiceClient>("AuthService");
    _ = builder.AddServiceClient<IEmployeeServiceClient, EmployeeServiceClient>("EmployeeService");
    _ = builder.AddServiceClient<INotificationServiceClient, NotificationServiceClient>("NotificationService");

    // Business Services
    _ = builder.Services.AddScoped<IOrderManagementService, OrderManagementService>();
    _ = builder.Services.AddScoped<IOrderAuthorizationService, OrderAuthorizationService>();
    _ = builder.Services.AddScoped<IOrderStatusService, OrderStatusService>();
    _ = builder.Services.AddScoped<IOrderFileService, OrderFileService>();
    _ = builder.Services.AddScoped<IOrderNoteService, OrderNoteService>();

    // Rate Limiting Configuration
    _ = builder.Services.AddRateLimiter(options =>
    {
        // General endpoints: 100 requests per minute partitioned by User
        _ = options.AddPolicy("general", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.User.GetUserId() ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 10
                }));

        // Batch operations: 10 requests per minute partitioned by User
        _ = options.AddPolicy("batch", httpContext =>
            RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: httpContext.User.GetUserId() ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                factory: _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 6,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 2
                }));

        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = 429;
            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                error = "Too many requests. Please try again later.",
                retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter) ? retryAfter.TotalSeconds : 60
            }, cancellationToken);
        };
    });

    // IAM Integration
    _ = builder.AddIAMServiceClient("order");
    _ = builder.Services.AddIAMRegistration<OrderIAMRegistrationService>("order");

    WebApplication app = builder.Build();
    ILogger<Program> logger = app.Services.GetRequiredService<ILogger<Program>>();

    // --- Database Migrations ---
    await app.MigrateDatabaseAsync<OrderDbContext>();

    // Middleware Pipeline
    _ = app.UseStandardMiddleware();

    if (!app.Environment.IsDevelopment())
    {
        _ = app.UseHttpsRedirection();
    }
    _ = app.UseCors();

    _ = app.UseAuthentication();
    _ = app.UseAuthorization();

    // Map endpoints after middleware
    _ = app.MapControllers();

    // Map Aspire default endpoints (/health, /alive, /metrics)
    _ = app.MapDefaultEndpoints(servicePrefix: "order");

    // Map OpenAPI and Scalar documentation (dev/staging only)
    _ = app.MapApiDocumentation(servicePrefix: "order");

    Log.ServiceStarted(logger, "Order Service");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.HostTerminated(bootstrapLogger, ex, "Order Service");
    // Force flush to ensure Aspire captures the error before process exits
    Console.Out.Flush();
    Console.Error.Flush();
    throw;
}
finally
{
    loggerFactory.Dispose();
}

/// <summary>
/// Main program class for the Order Service API.
/// </summary>
public partial class Program
{
    internal static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Starting {ServiceName} host")]
        public static partial void StartingHost(ILogger logger, string serviceName);

        [LoggerMessage(Level = LogLevel.Critical, Message = "{ServiceName} host terminated unexpectedly during startup")]
        public static partial void HostTerminated(ILogger logger, Exception ex, string serviceName);

        [LoggerMessage(Level = LogLevel.Information, Message = "{ServiceName} started successfully")]
        public static partial void ServiceStarted(ILogger logger, string serviceName);

        [LoggerMessage(Level = LogLevel.Error, Message = "Database migration failed - application may not function correctly")]
        public static partial void MigrationFailed(ILogger logger, Exception exception);
    }
}
