using Asp.Versioning;
using FluentValidation;
using HealthChecks.UI.Client;
using Maliev.OrderService.Api.Configuration;
using Maliev.OrderService.Api.Middleware;
using Maliev.OrderService.Api.Services.Business;
using Maliev.OrderService.Api.Services.External;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Extensions.Http;
using Serilog;
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

// Services
builder.Services.AddControllers();
builder.Services.AddMemoryCache(); // Simple configuration without SizeLimit
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
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
}).AddPolicyHandler(retryPolicy);

builder.Services.AddHttpClient<IMaterialServiceClient, MaterialServiceClient>((serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var options = config.GetSection("ExternalServices:MaterialService").Get<ExternalServiceOptions>()
        ?? throw new InvalidOperationException("ExternalServices:MaterialService configuration not found");

    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
}).AddPolicyHandler(retryPolicy);

builder.Services.AddHttpClient<IPaymentServiceClient, PaymentServiceClient>((serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var options = config.GetSection("ExternalServices:PaymentService").Get<ExternalServiceOptions>()
        ?? throw new InvalidOperationException("ExternalServices:PaymentService configuration not found");

    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
}).AddPolicyHandler(retryPolicy);

builder.Services.AddHttpClient<IUploadServiceClient, UploadServiceClient>((serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var options = config.GetSection("ExternalServices:UploadService").Get<ExternalServiceOptions>()
        ?? throw new InvalidOperationException("ExternalServices:UploadService configuration not found");

    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds); // Should be 300 for file uploads
}).AddPolicyHandler(retryPolicy);

builder.Services.AddHttpClient<IAuthServiceClient, AuthServiceClient>((serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var options = config.GetSection("ExternalServices:AuthService").Get<ExternalServiceOptions>()
        ?? throw new InvalidOperationException("ExternalServices:AuthService configuration not found");

    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
}).AddPolicyHandler(retryPolicy);

builder.Services.AddHttpClient<IEmployeeServiceClient, EmployeeServiceClient>((serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var options = config.GetSection("ExternalServices:EmployeeService").Get<ExternalServiceOptions>()
        ?? throw new InvalidOperationException("ExternalServices:EmployeeService configuration not found");

    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
}).AddPolicyHandler(retryPolicy);

builder.Services.AddHttpClient<INotificationServiceClient, NotificationServiceClient>((serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var options = config.GetSection("ExternalServices:NotificationService").Get<ExternalServiceOptions>()
        ?? throw new InvalidOperationException("ExternalServices:NotificationService configuration not found");

    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
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

// JWT Authentication Configuration
if (!builder.Environment.IsEnvironment("Testing"))
{
    var jwtSecret = builder.Configuration["Jwt:SecurityKey"] ?? throw new InvalidOperationException("Jwt:SecurityKey configuration not found");
    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer configuration not found");
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience configuration not found");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ClockSkew = TimeSpan.FromMinutes(5) // Allow 5 minutes clock skew
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

// OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Order Service API", Version = "v1" });
});

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
builder.Services.AddHealthChecks()
    .AddDbContextCheck<Maliev.OrderService.Data.OrderDbContext>(tags: readinessTags);

var app = builder.Build();

// Use path base for all routes (Kubernetes ingress routing)
app.UsePathBase("/orders");

// Middleware Pipeline (EXACT ORDER)
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.RoutePrefix = "swagger";
    c.SwaggerEndpoint("/orders/swagger/v1/swagger.json", "Order Service API v1");
});

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

// Make Program class accessible for integration tests
public partial class Program { }
