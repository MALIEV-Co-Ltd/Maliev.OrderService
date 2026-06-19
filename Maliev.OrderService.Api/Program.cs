using Maliev.Aspire.ServiceDefaults;
using Maliev.OrderService.Api.Consumers;
using Maliev.OrderService.Api.Services;
using Maliev.OrderService.Api.Services.Business;
using Maliev.OrderService.Api.Services.External;
using Maliev.OrderService.Infrastructure.Persistence;
using MassTransit;

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
    _ = builder.AddDefaultApiVersioning(); // API versioning with URL segment reader
    _ = builder.AddStandardMiddleware(options =>
    {
        options.EnableRequestLogging = true;
    });
    _ = builder.AddServiceMeters("orders-meter"); // Register service meters for OpenTelemetry business metrics

    _ = builder.AddStandardCache("order:"); // Redis + in-memory fallback, memory-optimized
    _ = builder.AddMassTransitWithRabbitMq(
        cfg =>
        {
            cfg.AddEntityFrameworkOutbox<OrderDbContext>(options =>
            {
                _ = options.UsePostgres();
                options.UseBusOutbox();
            });

            _ = cfg.AddConsumer<PaymentCompletedEventConsumer>();
            _ = cfg.AddConsumer<PaymentPendingEventConsumer>();
            _ = cfg.AddConsumer<PaymentCancelledEventConsumer>();
            _ = cfg.AddConsumer<PaymentFailedEventConsumer>();
            _ = cfg.AddConsumer<PaymentExpiredEventConsumer>();
            _ = cfg.AddConsumer<FileDeletedEventConsumer>();
            _ = cfg.AddConsumer<PreviewImagesGeneratedEventConsumer>();
            _ = cfg.AddConsumer<JobStatusChangedEventConsumer>();
        },
        configureRabbitMq: (context, rabbitMq) =>
        {
            rabbitMq.ReceiveEndpoint("order-payment-outcomes", endpoint =>
            {
                endpoint.ConfigureConsumer<PaymentCompletedEventConsumer>(context);
                endpoint.ConfigureConsumer<PaymentPendingEventConsumer>(context);
                endpoint.ConfigureConsumer<PaymentCancelledEventConsumer>(context);
                endpoint.ConfigureConsumer<PaymentFailedEventConsumer>(context);
                endpoint.ConfigureConsumer<PaymentExpiredEventConsumer>(context);
            });

            rabbitMq.ConfigureEndpoints(context);
        }); // RabbitMQ message bus with consumers
    _ = builder.AddPostgresDbContext<OrderDbContext>(connectionName: "OrderDbContext"); // PostgreSQL with retry logic

    // --- API Configuration ---
    _ = builder.AddStandardCors(); // CORS with fail-fast validation

    // JWT Authentication (tests override via PostConfigureAll with dynamic RSA keys)
    _ = builder.AddJwtAuthentication();
    _ = builder.Services.AddPermissionAuthorization();

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
    _ = builder.AddServiceClient<IGeometryServiceClient, GeometryServiceClient>("GeometryService");

    // Business Services
    _ = builder.Services.AddScoped<IOrderManagementService, OrderManagementService>();
    _ = builder.Services.AddScoped<IOrderAuthorizationService, OrderAuthorizationService>();
    _ = builder.Services.AddScoped<IOrderStatusService, OrderStatusService>();
    _ = builder.Services.AddScoped<IOrderFileService, OrderFileService>();
    _ = builder.Services.AddScoped<IOrderNoteService, OrderNoteService>();
    _ = builder.Services.AddScoped<IOrderPreviewImageService, OrderPreviewImageService>();

    // Rate Limiting (memory-optimized for low-spec nodes)
    _ = builder.AddStandardRateLimiting();

    // IAM Integration
    _ = builder.AddIAMServiceClient("order");
    _ = builder.Services.AddIAMRegistration<OrderIAMRegistrationService>("order");

    WebApplication app = builder.Build();
    ILogger<Program> logger = app.Services.GetRequiredService<ILogger<Program>>();

    // --- Database Migrations ---
    await app.MigrateDatabaseAsync<OrderDbContext>();
    using (IServiceScope seedScope = app.Services.CreateScope())
    {
        OrderDbContext dbContext = seedScope.ServiceProvider.GetRequiredService<OrderDbContext>();
        await OrderReferenceDataSeeder.SeedAsync(dbContext);
    }

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
