using Asp.Versioning;
using FluentValidation;
using HealthChecks.UI.Client;
using Maliev.OrderService.Api.Configuration;
using Maliev.OrderService.Api.Middleware;
using Maliev.OrderService.Api.Services.Business;
using Maliev.OrderService.Api.Services.External;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Extensions.Http;
using Scalar.AspNetCore;
using Serilog;
using StackExchange.Redis;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Serilog Configuration (Console only)
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
        formatProvider: System.Globalization.CultureInfo.InvariantCulture)
    .CreateLogger();

builder.Host.UseSerilog();

// Secrets from Google Secret Manager
var secretsPath = "/mnt/secrets";
if (Directory.Exists(secretsPath))
{
    builder.Configuration.AddKeyPerFile(directoryPath: secretsPath, optional: true);
}

// Redis Distributed Cache Configuration
var redisConnectionString = builder.Configuration["Redis:ConnectionString"];
var redisEnabled = bool.TryParse(builder.Configuration["Redis:Enabled"], out var isRedisEnabled) && isRedisEnabled;

if (redisEnabled && !string.IsNullOrEmpty(redisConnectionString) && !builder.Environment.IsEnvironment("Testing"))
{
    try
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "Order:";
        });

        var redis = ConnectionMultiplexer.Connect(redisConnectionString);
        builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

        Log.Information("Redis distributed cache configured: {RedisConnectionString}", redisConnectionString);
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Redis connection failed - will use in-memory cache fallback");
    }
}
else
{
    Log.Information("Redis disabled or not configured - using in-memory cache only");
}

builder.Services.AddMemoryCache(); // Fallback in-memory cache

// RabbitMQ Configuration (MassTransit)
var rabbitmqHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
var rabbitmqPort = int.TryParse(builder.Configuration["RabbitMQ:Port"], out var port) ? port : 5672;
var rabbitmqUser = builder.Configuration["RabbitMQ:Username"] ?? "guest";
var rabbitmqPassword = builder.Configuration["RabbitMQ:Password"] ?? "guest";
var rabbitmqVhost = builder.Configuration["RabbitMQ:VirtualHost"] ?? "/";
var rabbitmqEnabled = bool.TryParse(builder.Configuration["RabbitMQ:Enabled"], out var isRabbitmqEnabled) && isRabbitmqEnabled;

if (rabbitmqEnabled && !builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddMassTransit(x =>
    {
        // Add consumers here if needed in the future
        // x.AddConsumer<SomeEventConsumer>();

        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host(rabbitmqHost, (ushort)rabbitmqPort, rabbitmqVhost, h =>
            {
                h.Username(rabbitmqUser);
                h.Password(rabbitmqPassword);
            });

            cfg.ConfigureEndpoints(context);
        });
    });

    Log.Information("MassTransit configured with RabbitMQ: {Host}:{Port}", rabbitmqHost, rabbitmqPort);
}
else
{
    Log.Information("RabbitMQ/MassTransit disabled by configuration");
}

// Services
builder.Services.AddControllers();
builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// External Service HttpClients with Retry Policies (3 attempts, exponential backoff)
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

builder.Services.AddHttpClient<ICustomerServiceClient, CustomerServiceClient>((serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var options = config.GetSection("ExternalServices:CustomerService").Get<ExternalServiceOptions>()
        ?? throw new InvalidOperationException("ExternalServices:CustomerService configuration not found");

    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutInSeconds);
}).AddPolicyHandler(retryPolicy);

builder.Services.AddHttpClient<IMaterialServiceClient, MaterialServiceClient>((serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var options = config.GetSection("ExternalServices:MaterialService").Get<ExternalServiceOptions>()
        ?? throw new InvalidOperationException("ExternalServices:MaterialService configuration not found");

    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutInSeconds);
}).AddPolicyHandler(retryPolicy);

builder.Services.AddHttpClient<IPaymentServiceClient, PaymentServiceClient>((serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var options = config.GetSection("ExternalServices:PaymentService").Get<ExternalServiceOptions>()
        ?? throw new InvalidOperationException("ExternalServices:PaymentService configuration not found");

    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutInSeconds);
}).AddPolicyHandler(retryPolicy);

builder.Services.AddHttpClient<IUploadServiceClient, UploadServiceClient>((serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var options = config.GetSection("ExternalServices:UploadService").Get<ExternalServiceOptions>()
        ?? throw new InvalidOperationException("ExternalServices:UploadService configuration not found");

    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutInSeconds); // Should be 300 for file uploads
}).AddPolicyHandler(retryPolicy);

builder.Services.AddHttpClient<IAuthServiceClient, AuthServiceClient>((serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var options = config.GetSection("ExternalServices:AuthService").Get<ExternalServiceOptions>()
        ?? throw new InvalidOperationException("ExternalServices:AuthService configuration not found");

    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutInSeconds);
}).AddPolicyHandler(retryPolicy);

builder.Services.AddHttpClient<IEmployeeServiceClient, EmployeeServiceClient>((serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var options = config.GetSection("ExternalServices:EmployeeService").Get<ExternalServiceOptions>()
        ?? throw new InvalidOperationException("ExternalServices:EmployeeService configuration not found");

    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutInSeconds);
}).AddPolicyHandler(retryPolicy);

builder.Services.AddHttpClient<INotificationServiceClient, NotificationServiceClient>((serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var options = config.GetSection("ExternalServices:NotificationService").Get<ExternalServiceOptions>()
        ?? throw new InvalidOperationException("ExternalServices:NotificationService configuration not found");

    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutInSeconds);
}).AddPolicyHandler(retryPolicy);

// Business Services
builder.Services.AddScoped<IOrderManagementService, OrderManagementService>();
builder.Services.AddScoped<IOrderStatusService, OrderStatusService>();
builder.Services.AddScoped<IOrderFileService, OrderFileService>();
builder.Services.AddScoped<IOrderNoteService, OrderNoteService>();

// CORS Configuration (environment-based)
var corsOrigins = builder.Configuration["CORS_ALLOWED_ORIGINS"]?.Split(',') ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

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
        limiterOptions.SegmentsPerWindow = 6; // 10-second segments
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

// JWT Authentication Configuration (RSA Asymmetric Public Key Validation)
if (!builder.Environment.IsEnvironment("Testing"))
{
    var publicKeyBase64 = builder.Configuration["Jwt:PublicKey"];
    var issuer = builder.Configuration["Jwt:Issuer"] ?? (builder.Environment.IsDevelopment() ? "https://localhost:5001" : throw new InvalidOperationException("JWT Issuer not configured"));
    var audience = builder.Configuration["Jwt:Audience"] ?? (builder.Environment.IsDevelopment() ? "MalievApi" : throw new InvalidOperationException("JWT Audience not configured"));

    SecurityKey securityKey;
    if (string.IsNullOrEmpty(publicKeyBase64))
    {
        if (builder.Environment.IsDevelopment())
        {
            Log.Warning("JWT PublicKey not configured. Using dummy key for development. THIS IS INSECURE FOR PRODUCTION.");
            securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("SuperSecretKeyForDevelopmentOnly1234567890"));
        }
        else
        {
            throw new InvalidOperationException("JWT PublicKey not configured for non-development environment.");
        }
    }
    else
    {
        // The public key from Google Secret Manager is double base64-encoded PEM format
        // First decode to get the PEM string
        var pemString = Encoding.UTF8.GetString(Convert.FromBase64String(publicKeyBase64));

        // Extract base64 content from PEM format (remove headers)
        var base64Key = pemString
            .Replace("-----BEGIN PUBLIC KEY-----", "")
            .Replace("-----END PUBLIC KEY-----", "")
            .Replace("\r", "")
            .Replace("\n", "")
            .Trim();

        // Decode the base64 content to get raw DER bytes
        var keyBytes = Convert.FromBase64String(base64Key);

        // Import the DER-formatted public key
        var rsa = System.Security.Cryptography.RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(keyBytes, out _);
        securityKey = new RsaSecurityKey(rsa);
    }

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = securityKey,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    Log.Warning("JWT authentication failed: {Error}", context.Exception.Message);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    Log.Debug("JWT token validated for user: {UserId}", context.Principal?.Identity?.Name);
                    return Task.CompletedTask;
                }
            };
        });
}

// Authorization Policies (registered for all environments including Testing)
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

// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddMvc();

// OpenAPI
builder.Services.AddOpenApi();

// Database Configuration
if (!builder.Environment.IsEnvironment("Testing"))
{
    var connectionString = builder.Configuration.GetConnectionString("OrderDbContext")
        ?? throw new InvalidOperationException("Connection string 'OrderDbContext' not found.");

    builder.Services.AddDbContext<Maliev.OrderService.Data.OrderDbContext>(options =>
        options.UseNpgsql(connectionString));
}

// Health Checks
var readinessTags = new[] { "readiness" };
var healthChecksBuilder = builder.Services.AddHealthChecks()
    .AddDbContextCheck<Maliev.OrderService.Data.OrderDbContext>(tags: readinessTags);

// Add Redis health check if enabled
if (redisEnabled && !string.IsNullOrEmpty(redisConnectionString))
{
    healthChecksBuilder.AddRedis(redisConnectionString, "redis", tags: readinessTags);
}

// Add service defaults for .NET Aspire
builder.AddServiceDefaults();

var app = builder.Build();

// Use path base for all routes (Kubernetes ingress routing)
app.UsePathBase("/orders");

// Middleware Pipeline (EXACT ORDER)
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (!app.Environment.IsProduction())
{
    app.MapOpenApi("/orders/openapi/{documentName}.json");
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Maliev Order Service API")
            .WithTheme(Scalar.AspNetCore.ScalarTheme.Saturn)
            .WithDefaultHttpClient(Scalar.AspNetCore.ScalarTarget.CSharp, Scalar.AspNetCore.ScalarClient.HttpClient)
            .WithEndpointPrefix("/orders/scalar/{documentName}")
            .WithOpenApiRoutePattern("/orders/openapi/{documentName}.json");
    });

    Log.Information("Scalar API documentation enabled at /orders/scalar/v1");
}

app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Health checks
app.MapGet("/liveness", () => "Healthy")
   .WithName("Liveness")
   .ExcludeFromDescription()
   .AllowAnonymous();

app.MapHealthChecks("/readiness", new HealthCheckOptions
{
    Predicate = healthCheck => healthCheck.Tags.Contains("readiness"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).ExcludeFromDescription()
  .AllowAnonymous();

app.MapControllers();

try
{
    Log.Information("Starting Order Service API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Order Service API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
