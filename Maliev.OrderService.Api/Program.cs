using Maliev.OrderService.Api.Services.Business;
using Maliev.OrderService.Api.Services.External;
using Maliev.OrderService.Api.Services; // Added
using Maliev.OrderService.Api.Authorization; // Added
using Maliev.Aspire.ServiceDefaults; // Added
using Maliev.OrderService.Data;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// --- Secrets & Configuration ---
builder.AddGoogleSecretManagerVolume(); // Load secrets from /mnt/secrets if available

// --- Infrastructure & Observability ---
builder.AddServiceDefaults(); // OpenTelemetry, health checks, resilience
builder.AddStandardMiddleware(options =>
{
    options.EnableRequestLogging = true;
});
builder.AddServiceMeters("orders-meter"); // Register service meters for OpenTelemetry business metrics

builder.AddRedisDistributedCache(instanceName: "order:"); // Redis with in-memory fallback
builder.AddMassTransitWithRabbitMq(); // RabbitMQ message bus (non-blocking startup)
builder.AddPostgresDbContext<OrderDbContext>(connectionName: "OrderDbContext"); // PostgreSQL with retry logic

// --- API Configuration ---
builder.AddDefaultCors(); // CORS from CORS:AllowedOrigins config
builder.AddDefaultApiVersioning(); // API versioning with URL segment reader

// JWT Authentication (tests override via PostConfigureAll with dynamic RSA keys)
builder.AddJwtAuthentication();
builder.Services.AddPermissionAuthorization();

// Add specific policies
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Admin", policy => 
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("role", "admin");
    });

// Add OpenAPI (must be in Program.cs for XML comments to work via source generator)
if (!builder.Environment.IsProduction())
{
    builder.AddStandardOpenApi(
        title: "MALIEV Order Service API",
        description: "Sales order processing service. Manages order lifecycle from creation to fulfillment, batch order operations, status history tracking, file attachments, internal notes, and cancellation with reason tracking.");
}

builder.Services.AddControllers();
// External Service HttpClients with Standard Resilience Handler
builder.AddServiceClient<ICustomerServiceClient, CustomerServiceClient>("CustomerService");
builder.AddServiceClient<IMaterialServiceClient, MaterialServiceClient>("MaterialService");
builder.AddServiceClient<IPaymentServiceClient, PaymentServiceClient>("PaymentService");
builder.AddServiceClient<IUploadServiceClient, UploadServiceClient>("UploadService");
builder.AddServiceClient<IAuthServiceClient, AuthServiceClient>("AuthService");
builder.AddServiceClient<IEmployeeServiceClient, EmployeeServiceClient>("EmployeeService");
builder.AddServiceClient<INotificationServiceClient, NotificationServiceClient>("NotificationService");

// Business Services
builder.Services.AddScoped<IOrderManagementService, OrderManagementService>();
builder.Services.AddScoped<IOrderStatusService, OrderStatusService>();
builder.Services.AddScoped<IOrderFileService, OrderFileService>();
builder.Services.AddScoped<IOrderNoteService, OrderNoteService>();

// Rate Limiting Configuration
builder.Services.AddRateLimiter(options =>
{
    // General endpoints: 100 requests per minute per IP
    options.AddFixedWindowLimiter("general", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 10;
    });

    // Batch operations: 10 requests per minute per IP (more restrictive)
    options.AddSlidingWindowLimiter("batch", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.SegmentsPerWindow = 6;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 2;
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Too many requests. Please try again later.",
            retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter) ? retryAfter.TotalSeconds : 60
        }, cancellationToken);
    };
});

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Run database migrations on startup (skip in Testing environment)
if (!app.Environment.IsEnvironment("Testing"))
{
    try
    {
        await app.MigrateDatabaseAsync<OrderDbContext>();
    }
    catch (Exception ex)
    {
        Log.MigrationFailed(logger, ex);
        // Don't throw - allow app to start for debugging
    }
}

// Middleware Pipeline
app.UseStandardMiddleware();

app.UseHttpsRedirection();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// Map endpoints after middleware
app.MapControllers();

// Map Aspire default endpoints (/health, /alive, /metrics)
app.MapDefaultEndpoints(servicePrefix: "order");

// Map OpenAPI and Scalar documentation (dev/staging only)
app.MapApiDocumentation(servicePrefix: "order");

Log.ServiceStarted(logger);
await app.RunAsync();

/// <summary>
/// Main program class for the Order Service API.
/// </summary>
public partial class Program
{
    /// <summary>
    /// High-performance logger message definitions for startup.
    /// </summary>
    internal static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "OrderService started successfully")]
        public static partial void ServiceStarted(ILogger logger);

        [LoggerMessage(Level = LogLevel.Error, Message = "Database migration failed - application may not function correctly")]
        public static partial void MigrationFailed(ILogger logger, Exception exception);
    }
}

