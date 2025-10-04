# Maliev.OrderService Development Guidelines

Auto-generated from all feature plans. Last updated: 2025-10-02

## Active Technologies
- C# / .NET 9.0 + ASP.NET Core 9.0, Entity Framework Core 9.0.9, Npgsql 9.0.2, Serilog 8.0.2, AutoMapper, FluentValidation (001-create-a-comprehensive)

## Project Structure
```
backend/
frontend/
tests/
```

## Commands
# Add commands for C# / .NET 9.0

## Code Style
C# / .NET 9.0: Follow standard conventions

## Recent Changes
- 001-create-a-comprehensive: Added C# / .NET 9.0 + ASP.NET Core 9.0, Entity Framework Core 9.0.9, Npgsql 9.0.2, Serilog 8.0.2, AutoMapper, FluentValidation

<!-- MANUAL ADDITIONS START -->

## Order Service Patterns

### Database
- PostgreSQL 18: `order_app_db` database
- EF Core 9.0.9 with Npgsql 9.0.2
- Optimistic concurrency: `RowVersion` byte[] on Order entity
- Migrations: Auto-apply on startup via `dotnet ef database update`

### 16-State Order Workflow
**Primary Flow**: New → Reviewing → [Rejected|Reviewed] → Quoted → [Declined|Accepted|Expired] → [Paid|POIssued] → InProgress → Finished → Shipped
**Exception Flows**: InProgress ↔ OnHold, Finished/Shipped → Reopen → InProgress, Any → Cancelled

**Validation**: `ValidTransitions` dictionary enforces state rules
```csharp
Dictionary<OrderStatusEnum, List<OrderStatusEnum>> ValidTransitions = new()
{
    [New] = [Reviewing, Cancelled],
    [Reviewing] = [Rejected, Reviewed, Cancelled],
    // ... 16 total states
};
```

### External Service Integration (IConfiguration Pattern)
- **Customer Service**: `ExternalServices:CustomerService` - NDA validation, customer details
- **Material Service**: `ExternalServices:MaterialService` - Material details, pricing
- **Payment Service**: `ExternalServices:PaymentService` - Payment status, refunds, partial charges
- **Upload Service**: `ExternalServices:UploadService` - File operations with retry (3 attempts, exponential backoff)
- **Auth Service**: `ExternalServices:AuthService` - JWT validation, user context (`{ userType, userId, roles }`)
- **Employee Service**: `ExternalServices:EmployeeService` - Employee details, department listing
- **Notification Service**: `ExternalServices:NotificationService` - Multi-channel notifications (LINE, email)

**Configuration Class**:
```csharp
// Configuration/ExternalServiceOptions.cs
public class ExternalServiceOptions
{
    public required string BaseUrl { get; set; }
    public int TimeoutSeconds { get; set; } = 180;
}
```

**HttpClient Pattern**:
```csharp
builder.Services.AddHttpClient<ICustomerServiceClient, CustomerServiceClient>((serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var options = config.GetSection("ExternalServices:CustomerService").Get<ExternalServiceOptions>()
        ?? throw new InvalidOperationException("ExternalServices:CustomerService configuration not found");

    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
}).AddPolicyHandler(retryPolicy);
```

### RBAC Context-Based Authorization
- Auth Service returns: `{ userType: "customer"|"employee", userId, roles: [] }`
- **Customer**: Own orders only (`Order.CustomerId = UserContext.UserId`)
- **Employee**: Assigned orders only (`Order.AssignedEmployeeId = UserContext.UserId`)
- **Manager**: Department orders (`Order.DepartmentId = UserContext.DepartmentId`)
- **Admin**: All orders (no restrictions)

### Batch Operations
- All-or-nothing transactions: `DbContext.Database.BeginTransactionAsync()`
- Rollback on any failure, return detailed error with failed item index
- Optimistic locking: Check `version` (RowVersion) for concurrent updates

### File Management
- Max 100MB per file, 500MB total per order
- Supported formats: CAD (STL, STEP, IGES, OBJ), Images (JPG, PNG, PDF), Docs (PDF, DOC, DOCX), Archives (ZIP, RAR, 7Z)
- Upload Service integration: ObjectPath pattern `orders/{orderId}/files/{filename}`
- 30-day retention: Background Service soft-deletes files after order completion

### Audit Logging
- Append-only `AuditLog` table (7-year retention)
- All sensitive operations logged: `{ Action, PerformedBy, PerformedAt, EntityType, EntityId, ChangeDetails (JSON) }`
- Confidential order access always audited

### Configuration Security (Google Secret Manager)

**Secret Naming Convention**:
- Database: `ConnectionStrings__OrderDbContext`
- JWT: `Jwt__SecurityKey`, `Jwt__Issuer`, `Jwt__Audience`
- External Services: `ExternalServices__<ServiceName>__BaseUrl`, `ExternalServices__<ServiceName>__TimeoutSeconds`
- CORS: `CORS_ALLOWED_ORIGINS` (flat variable, comma-separated)

**Double Underscore (`__`) Conversion**:
- Mounted at `/mnt/secrets` via Google Secret Manager
- Automatically converted to colon (`:`) in IConfiguration
- Example: `Jwt__SecurityKey` → `builder.Configuration["Jwt:SecurityKey"]`

**Environment-Specific Issuers**:
- Development: `maliev-dev`
- Staging: `maliev-staging`
- Production: `maliev-prod`

**Configuration Pattern**:
```csharp
// Secrets from Google Secret Manager
var secretsPath = "/mnt/secrets";
if (Directory.Exists(secretsPath))
{
    builder.Configuration.AddKeyPerFile(directoryPath: secretsPath, optional: true);
}

// JWT Configuration (IConfiguration pattern)
var jwtSecret = builder.Configuration["Jwt:SecurityKey"]
    ?? throw new InvalidOperationException("Jwt:SecurityKey configuration not found");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer configuration not found");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience configuration not found");
```

**Critical Rules**:
- **NEVER** hardcode URLs, connection strings, or secrets
- **NEVER** use `Environment.GetEnvironmentVariable()` - use `IConfiguration` binding instead
- **ALWAYS** use `ExternalServiceOptions` class for external services
- **ALWAYS** validate required configuration with null-coalescing operator (`??`)

<!-- MANUAL ADDITIONS END -->