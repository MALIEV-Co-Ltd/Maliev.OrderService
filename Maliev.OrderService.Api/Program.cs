using Maliev.OrderService.Api.Services.Business;
using Maliev.OrderService.Api.Services.External;
using Maliev.OrderService.Data;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// --- Secrets & Configuration ---
builder.AddGoogleSecretManagerVolume(); // Load secrets from /mnt/secrets if available

// --- Infrastructure & Observability ---
builder.AddServiceDefaults(); // OpenTelemetry, health checks, resilience
builder.AddServiceMeters("orders-meter"); // Register service meters for OpenTelemetry business metrics

builder.AddRedisDistributedCache(instanceName: "order:"); // Redis with in-memory fallback
builder.AddMassTransitWithRabbitMq(); // RabbitMQ message bus (non-blocking startup)
builder.AddPostgresDbContext<OrderDbContext>(connectionStringName: "OrderDbContext"); // PostgreSQL with retry logic

// --- API Configuration ---
builder.AddDefaultCors(); // CORS from CORS:AllowedOrigins config
builder.AddDefaultApiVersioning(); // API versioning with URL segment reader

// JWT Authentication (tests override via PostConfigureAll with dynamic RSA keys)
builder.AddJwtAuthentication();

// Add OpenAPI (must be in Program.cs for XML comments to work via source generator)
if (!builder.Environment.IsProduction())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi("v1", options =>
    {
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            document.Info.Title = "MALIEV Order Service API";
            document.Info.Version = "v1";
            document.Info.Description = "Sales order processing service. Manages order lifecycle from creation to fulfillment, batch order operations, status history tracking, file attachments, internal notes, and cancellation with reason tracking.";
            return Task.CompletedTask;
        });
    });
}

builder.Services.AddControllers();
// External Service HttpClients with Standard Resilience Handler
builder.Services.Configure<Maliev.OrderService.Api.Configuration.ExternalServiceOptions>(
    builder.Configuration.GetSection("ExternalServices:CustomerService"));
builder.Services.AddHttpClient<ICustomerServiceClient, CustomerServiceClient>()
    .AddStandardResilienceHandler();

builder.Services.Configure<Maliev.OrderService.Api.Configuration.ExternalServiceOptions>(
    builder.Configuration.GetSection("ExternalServices:MaterialService"));
builder.Services.AddHttpClient<IMaterialServiceClient, MaterialServiceClient>()
    .AddStandardResilienceHandler();

builder.Services.Configure<Maliev.OrderService.Api.Configuration.ExternalServiceOptions>(
    builder.Configuration.GetSection("ExternalServices:PaymentService"));
builder.Services.AddHttpClient<IPaymentServiceClient, PaymentServiceClient>()
    .AddStandardResilienceHandler();

builder.Services.Configure<Maliev.OrderService.Api.Configuration.ExternalServiceOptions>(
    builder.Configuration.GetSection("ExternalServices:UploadService"));
builder.Services.AddHttpClient<IUploadServiceClient, UploadServiceClient>()
    .AddStandardResilienceHandler();

builder.Services.Configure<Maliev.OrderService.Api.Configuration.ExternalServiceOptions>(
    builder.Configuration.GetSection("ExternalServices:AuthService"));
builder.Services.AddHttpClient<IAuthServiceClient, AuthServiceClient>()
    .AddStandardResilienceHandler();

builder.Services.Configure<Maliev.OrderService.Api.Configuration.ExternalServiceOptions>(
    builder.Configuration.GetSection("ExternalServices:EmployeeService"));
builder.Services.AddHttpClient<IEmployeeServiceClient, EmployeeServiceClient>()
    .AddStandardResilienceHandler();

builder.Services.Configure<Maliev.OrderService.Api.Configuration.ExternalServiceOptions>(
    builder.Configuration.GetSection("ExternalServices:NotificationService"));
builder.Services.AddHttpClient<INotificationServiceClient, NotificationServiceClient>()
    .AddStandardResilienceHandler();

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

// Authorization Policies
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Customer", policy => policy.RequireClaim("userType", "customer"))
    .AddPolicy("Employee", policy => policy.RequireClaim("userType", "employee"))
    .AddPolicy("Manager", policy => policy.RequireClaim(System.Security.Claims.ClaimTypes.Role, "Manager"))
    .AddPolicy("Admin", policy => policy.RequireClaim(System.Security.Claims.ClaimTypes.Role, "Admin"))
    .AddPolicy("EmployeeOrHigher", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("userType", "employee") ||
            context.User.HasClaim(System.Security.Claims.ClaimTypes.Role, "Manager") ||
            context.User.HasClaim(System.Security.Claims.ClaimTypes.Role, "Admin")));

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

