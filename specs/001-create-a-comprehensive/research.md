# Research & Technology Decisions: Order Service

**Feature**: Order Service API for Rapid Prototyping & Manufacturing
**Date**: 2025-10-02
**Phase**: Phase 0 - Outline & Research

## Executive Summary

This document captures technical research and technology decisions for the Order Service microservice. All decisions align with MALIEV constitution principles and prioritize maintainability, performance, and operational simplicity.

---

## 1. .NET 9.0 Microservice Architecture Patterns

### Decision: Controller-Based WebAPI with Layered Architecture

**Rationale**:
- **MALIEV Standards Compliance**: Existing services use controller-based approach (documented in `CLAUDE.md`)
- **Team Familiarity**: Development team experienced with MVC pattern and attribute routing
- **Explicit Contracts**: Controllers with `[ApiController]` attribute provide automatic OpenAPI generation
- **Separation of Concerns**: Clear layering (Controllers → Services → Data) aids testing and maintenance

**Alternatives Considered**:
- **Minimal APIs**: Rejected - Less explicit contracts, harder to generate comprehensive Swagger docs, newer pattern unfamiliar to team
- **CQRS with MediatR**: Rejected - Adds complexity for read/write operations without proportional value at current scale (YAGNI principle)

**Implementation Notes**:
- Use `[ApiController]` attribute for automatic model validation
- Structure: `Controllers/` → `Services/` → `Data/`
- Follow MALIEV naming: `Maliev.OrderService.Api`, `Maliev.OrderService.Data`, `Maliev.OrderService.Tests`

---

## 2. Entity Framework Core 9.0 Optimistic Concurrency

### Decision: RowVersion (byte[]) with Automatic Conflict Detection

**Rationale**:
- **Automatic Management**: EF Core automatically manages `RowVersion` on each update
- **Database-Level Support**: PostgreSQL `xmin` system column provides built-in versioning
- **Concurrency Detection**: EF Core throws `DbUpdateConcurrencyException` on version mismatch
- **Performance**: Binary comparison faster than timestamp string comparison

**Alternatives Considered**:
- **Manual Timestamp**: Rejected - Requires manual version increment, prone to human error
- **Pessimistic Locking**: Rejected - Requires transaction locks, reduces concurrency, increases deadlock risk

**Implementation Notes**:
```csharp
// Order entity
[Timestamp]
public byte[] Version { get; set; }

// Update handling
try
{
    await _context.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException)
{
    return Conflict(new { error = "Order was modified by another user" });
}
```

---

## 3. Multi-Service Integration with HttpClient

### Decision: IHttpClientFactory with Named Clients + Polly Retry Policies

**Rationale**:
- **DNS Refresh**: `IHttpClientFactory` manages handler lifetime, prevents DNS stale issues
- **Resource Management**: Automatic connection pooling, prevents socket exhaustion
- **Resilience**: Polly policies provide retry, circuit breaker, timeout strategies
- **Configuration**: Environment-based URLs via `IConfiguration` (Google Secret Manager)

**Alternatives Considered**:
- **Direct HttpClient Instantiation**: Rejected - Socket exhaustion, no DNS refresh, manual disposal
- **RestSharp/Refit**: Rejected - Additional dependency, typed client overkill for simple REST calls

**Implementation Notes**:
```csharp
// Service registration
services.AddHttpClient("CustomerService", client =>
{
    var baseUrl = Environment.GetEnvironmentVariable("CUSTOMER_SERVICE_URL")
                  ?? throw new InvalidOperationException("CUSTOMER_SERVICE_URL not configured");
    client.BaseAddress = new Uri(baseUrl);
})
.AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3,
    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

// 7 named clients: CustomerService, MaterialService, PaymentService, UploadService, AuthService, EmployeeService, NotificationService
```

---

## 4. State Machine Implementation for Order Workflow

### Decision: Enum-Based States with Transition Validation Dictionary

**Rationale**:
- **Type Safety**: Enum prevents invalid state strings
- **Explicit Transitions**: Dictionary defines all valid state changes in one location
- **Maintainability**: Single source of truth for workflow rules
- **Performance**: O(1) dictionary lookup vs complex conditional logic

**Alternatives Considered**:
- **State Pattern (OOP)**: Rejected - Over-engineering for 16 states, increases class count, harder to visualize workflow
- **Workflow Engine (Elsa/WorkflowCore)**: Rejected - External dependency, learning curve, overkill for linear workflow

**Implementation Notes**:
```csharp
public enum OrderStatusEnum
{
    New, Reviewing, Rejected, Reviewed, Quoted, Declined, Accepted,
    Expired, Paid, POIssued, InProgress, OnHold, Finished, Shipped,
    Reopen, Cancelled
}

private static readonly Dictionary<OrderStatusEnum, List<OrderStatusEnum>> ValidTransitions = new()
{
    [OrderStatusEnum.New] = [OrderStatusEnum.Reviewing, OrderStatusEnum.Cancelled],
    [OrderStatusEnum.Reviewing] = [OrderStatusEnum.Rejected, OrderStatusEnum.Reviewed, OrderStatusEnum.Cancelled],
    // ... 16 total entries
};

public bool IsValidTransition(OrderStatusEnum from, OrderStatusEnum to)
{
    return ValidTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
}
```

---

## 5. Batch Operations with All-or-Nothing Transactions

### Decision: Explicit DbContext Transaction with Try-Catch Rollback

**Rationale**:
- **EF Core Native**: Built-in transaction support, no external dependencies
- **Explicit Control**: Clear rollback logic, easier debugging
- **Error Reporting**: Can return detailed failure information per item before rollback
- **Async Support**: `BeginTransactionAsync` supports async batch operations

**Alternatives Considered**:
- **TransactionScope**: Rejected - Can escalate to distributed transaction, overkill for single-DB operations
- **SaveChanges per Item**: Rejected - Violates all-or-nothing requirement, leaves partial state

**Implementation Notes**:
```csharp
public async Task<Result<List<OrderDto>>> BatchCreateAsync(List<CreateOrderRequest> requests)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        var orders = new List<Order>();
        foreach (var request in requests)
        {
            // Validate each request (throws on failure)
            await ValidateCustomerExistsAsync(request.CustomerId);
            orders.Add(MapToEntity(request));
        }

        await _context.Orders.AddRangeAsync(orders);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Result.Success(orders.Select(MapToDto).ToList());
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        return Result.Failure<List<OrderDto>>(ex.Message);
    }
}
```

---

## 6. File Upload Integration with Upload Service

### Decision: Polly Exponential Backoff with 3 Retries

**Rationale**:
- **Transient Failure Handling**: Network issues, temporary service unavailability
- **Exponential Backoff**: 2^attempt seconds (2s, 4s, 8s) reduces server load during recovery
- **Spec Compliance**: Matches FR-019 requirement for 3 automatic retries
- **Multipart Form Data**: `MultipartFormDataContent` for `IFormFile` upload to Upload Service

**Alternatives Considered**:
- **Fixed Delay Retry**: Rejected - Can overwhelm recovering service, less effective than exponential
- **Circuit Breaker Only**: Rejected - Doesn't handle transient failures, requires multiple failures to open

**Implementation Notes**:
```csharp
// Polly policy configuration
services.AddHttpClient("UploadService", client =>
{
    client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("UPLOAD_SERVICE_URL"));
})
.AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(
    retryCount: 3,
    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
    onRetry: (outcome, timespan, retryCount, context) =>
    {
        _logger.LogWarning($"Upload retry {retryCount} after {timespan.TotalSeconds}s");
    }
));

// File upload
var content = new MultipartFormDataContent();
content.Add(new StreamContent(file.OpenReadStream()), "File", file.FileName);
content.Add(new StringContent($"orders/{orderId}/files/{file.FileName}"), "ObjectPath");
content.Add(new StringContent("Internal"), "AccessLevel");

var response = await _uploadClient.PostAsync("/v1/files", content);
```

---

## 7. JWT Authentication with Auth Service Integration

### Decision: Custom Middleware for Auth Service `/validate` Endpoint

**Rationale**:
- **Context-Based Authorization**: Auth Service returns `{ userType, userId, roles }` in single call
- **Dual-DB Support**: Abstracts Customer DB vs Employee DB complexity at Auth Service level
- **Centralized Logic**: Order Service implements RBAC based on returned context
- **Flexible**: Can add custom claims (e.g., `departmentId`) without changing JWT structure

**Alternatives Considered**:
- **Built-in JWT Bearer Validation**: Rejected - Requires decoding JWT locally, doesn't support Auth Service validation call
- **API Gateway Auth**: Rejected - Order Service must independently validate for direct calls (health checks, webhooks)

**Implementation Notes**:
```csharp
public class JwtAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHttpClientFactory _clientFactory;

    public async Task InvokeAsync(HttpContext context)
    {
        var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        if (string.IsNullOrEmpty(token))
        {
            context.Response.StatusCode = 401;
            return;
        }

        var authClient = _clientFactory.CreateClient("AuthService");
        var response = await authClient.PostAsJsonAsync("/v1/validate", new { token });

        if (!response.IsSuccessStatusCode)
        {
            context.Response.StatusCode = 401;
            return;
        }

        var userContext = await response.Content.ReadFromJsonAsync<UserContext>();
        context.Items["UserContext"] = userContext; // Available in controllers

        await _next(context);
    }
}

public class UserContext
{
    public string UserType { get; set; } // "customer" | "employee"
    public string UserId { get; set; }
    public List<string> Roles { get; set; } // ["Customer"] or ["Employee", "Manager"]
}
```

---

## 8. CORS Configuration for Environment-Specific Origins

### Decision: IConfiguration-Based Origin Arrays with Environment Detection

**Rationale**:
- **Environment Isolation**: Dev/Staging/Prod have different allowed origins
- **Security**: Explicit origin whitelist prevents unauthorized cross-origin access
- **Configuration**: Origins from environment variables (Google Secret Manager)
- **Flexibility**: Easy to add new frontend domains without code changes

**Alternatives Considered**:
- **Hardcoded Origins**: Rejected - Violates secrets management principle, not environment-agnostic
- **Wildcard CORS**: Rejected - Security risk, allows any origin in production

**Implementation Notes**:
```csharp
// appsettings.json (non-sensitive, example only)
{
  "Cors": {
    "AllowedOrigins": "${CORS_ALLOWED_ORIGINS}" // Placeholder
  }
}

// Program.cs
var allowedOrigins = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS")?.Split(',')
    ?? new[] { "http://localhost:3000" }; // Dev fallback

builder.Services.AddCors(options =>
{
    options.AddPolicy("MalievCorsPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
              .WithHeaders("Authorization", "Content-Type")
              .AllowCredentials(); // If frontend needs cookies
    });
});

// Environment variable examples (from Google Secret Manager):
// Dev: "https://dev.intranet.maliev.com,https://dev.www.maliev.com"
// Staging: "https://staging.intranet.maliev.com,https://staging.www.maliev.com"
// Prod: "https://intranet.maliev.com,https://www.maliev.com"
```

---

## 9. Audit Logging with Append-Only Table Design

### Decision: Separate AuditLog Table with Indexed Columns

**Rationale**:
- **7-Year Retention**: Dedicated table simplifies retention policy enforcement
- **Query Performance**: Indexed `OrderId`, `PerformedAt`, `PerformedBy` for fast audit queries
- **Immutability**: No updates/deletes on audit records, append-only pattern
- **JSON Details**: `ChangeDetails` column stores before/after state as JSON for flexibility

**Alternatives Considered**:
- **Temporal Tables**: Rejected - Complex queries, harder to manage retention, PostgreSQL support limited
- **Event Sourcing**: Rejected - Over-engineering, requires event store, replay complexity

**Implementation Notes**:
```csharp
public class AuditLog
{
    public long AuditId { get; set; }
    public string OrderId { get; set; }
    public AuditAction Action { get; set; } // Create, Update, Cancel, StatusChange, FileUpload
    public string PerformedBy { get; set; } // UserId from UserContext
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    public string EntityType { get; set; } // "Order", "OrderStatus", "OrderFile"
    public string EntityId { get; set; }
    public string ChangeDetails { get; set; } // JSON: { "before": {...}, "after": {...} }
}

// EF Core configuration
modelBuilder.Entity<AuditLog>(entity =>
{
    entity.HasKey(e => e.AuditId);
    entity.HasIndex(e => e.OrderId);
    entity.HasIndex(e => e.PerformedAt);
    entity.HasIndex(e => e.PerformedBy);
    entity.Property(e => e.ChangeDetails).HasColumnType("jsonb"); // PostgreSQL JSONB
});

// Usage
await _context.AuditLogs.AddAsync(new AuditLog
{
    OrderId = order.OrderId,
    Action = AuditAction.Update,
    PerformedBy = userContext.UserId,
    EntityType = "Order",
    EntityId = order.OrderId,
    ChangeDetails = JsonSerializer.Serialize(new { before = oldOrder, after = newOrder })
});
```

---

## 10. Data Retention with Background Service Cleanup

### Decision: IHostedService with Scoped DbContext for Periodic File Deletion

**Rationale**:
- **30-Day Policy**: FR-041 requires automatic file deletion 30 days after order completion
- **Background Execution**: `IHostedService` runs independently of API requests
- **Scoped Context**: Creates new `DbContext` per cleanup cycle, prevents context lifetime issues
- **Configurable Interval**: Timer-based execution (e.g., daily at 2 AM)

**Alternatives Considered**:
- **Hangfire/Quartz**: Rejected - External dependency, overkill for single periodic task
- **Manual Cleanup Script**: Rejected - Requires external scheduler, violates service autonomy

**Implementation Notes**:
```csharp
public class FileCleanupService : IHostedService, IDisposable
{
    private Timer _timer;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FileCleanupService> _logger;

    public FileCleanupService(IServiceProvider serviceProvider, ILogger<FileCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromDays(1)); // Run daily
        return Task.CompletedTask;
    }

    private async void DoWork(object state)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var uploadClient = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("UploadService");

        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        var filesToDelete = await context.OrderFiles
            .Where(f => f.DeletedAt == null &&
                        f.Order.Status == OrderStatusEnum.Shipped &&
                        f.Order.UpdatedAt < cutoffDate)
            .ToListAsync();

        foreach (var file in filesToDelete)
        {
            // Call Upload Service to delete from GCS
            await uploadClient.DeleteAsync($"/v1/files/path?objectPath={file.ObjectPath}");

            // Soft delete in database
            file.DeletedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
        _logger.LogInformation($"Deleted {filesToDelete.Count} files older than 30 days");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose() => _timer?.Dispose();
}

// Register in Program.cs
builder.Services.AddHostedService<FileCleanupService>();
```

---

## 11. Material Service Integration with Reference + Cache Pattern

### Decision: Integer References with Denormalized Display Names and 24-Hour TTL

**Rationale**:
- **Service Autonomy**: Store integer references (materialId, colorId, surfaceFinishingId) to avoid duplicating Material Service data
- **Validation**: Validate selections via GET endpoints (`/materials/{id}`, `/materials/{id}/colors`, etc.) - no dedicated `/validate` endpoint needed
- **Performance**: Cache display names (materialName, colorName, surfaceFinishingName) for 24 hours to reduce Material Service calls
- **Consistency**: Re-fetch from Material Service if cache expires to ensure data accuracy
- **Domain Boundaries**: Order Service does NOT calculate costs - Quoting Service handles material cost calculations

**Alternatives Considered**:
- **Text Fields Only**: Rejected - Duplicates Material Service data, no validation, inconsistent naming
- **Integer References Only (No Cache)**: Rejected - Every order list/detail view requires Material Service call, poor performance
- **Long TTL (7+ days)**: Rejected - Stale data if Material Service updates material names

**Implementation Notes**:
```csharp
// Order entity
public int? MaterialId { get; set; }
public int? ColorId { get; set; }
public int? SurfaceFinishingId { get; set; }

// Cached display names (updated every 24 hours)
public string? MaterialName { get; set; }
public string? ColorName { get; set; }
public string? SurfaceFinishingName { get; set; }

// Validation during order creation
public async Task<bool> ValidateMaterialAsync(int materialId, int colorId, int surfaceFinishingId)
{
    var materialClient = _clientFactory.CreateClient("MaterialService");

    // Validate material exists
    var materialResponse = await materialClient.GetAsync($"/v1/materials/{materialId}");
    if (!materialResponse.IsSuccessStatusCode) return false;

    // Validate color available for material
    var colorsResponse = await materialClient.GetAsync($"/v1/materials/{materialId}/colors");
    var colors = await colorsResponse.Content.ReadFromJsonAsync<List<ColorDto>>();
    if (!colors.Any(c => c.ColorId == colorId)) return false;

    // Validate finishing available for material/color combo
    var finishingsResponse = await materialClient.GetAsync($"/v1/materials/{materialId}/surface-finishings?colorId={colorId}");
    var finishings = await finishingsResponse.Content.ReadFromJsonAsync<List<SurfaceFinishingDto>>();
    if (!finishings.Any(f => f.SurfaceFinishingId == surfaceFinishingId)) return false;

    return true;
}

// Cache refresh service (IHostedService)
public async Task RefreshMaterialCacheAsync()
{
    var orders = await _context.Orders
        .Where(o => o.MaterialId != null &&
                    (o.MaterialCacheUpdatedAt == null || o.MaterialCacheUpdatedAt < DateTime.UtcNow.AddHours(-24)))
        .ToListAsync();

    foreach (var order in orders)
    {
        var material = await _materialClient.GetMaterialAsync(order.MaterialId.Value);
        order.MaterialName = material.Name;
        order.MaterialCacheUpdatedAt = DateTime.UtcNow;
    }

    await _context.SaveChangesAsync();
}
```

---

## 12. Service-Specific Attributes with Separate Tables

### Decision: One Table Per Service Type for Queryable Attributes

**Rationale**:
- **Query Performance**: Standard SQL queries on typed columns (e.g., `WHERE tolerance = '±0.05mm'`) vs complex JSONB queries
- **Type Safety**: Database enforces data types (boolean, string, integer), JSONB stores everything as text
- **Indexing**: Can index specific columns (e.g., `tolerance`, `complexity_level`) for fast filtering
- **Schema Validation**: Database schema documents required fields per service type
- **Query Examples**: "All CNC orders with ±0.05mm tolerance", "All 3D Design orders with complexity=High"

**Alternatives Considered**:
- **JSONB Column**: Rejected - Hard to query, no type safety, requires GIN indexes, complex WHERE clauses like `WHERE attributes->>'tolerance' = '±0.05mm'`
- **EAV Pattern**: Rejected - 3 tables (Entity-Attribute-Value), joins required, poor performance
- **Shared Attributes Table**: Rejected - Many nullable columns (50+ columns for 10 service types), sparse rows

**Implementation Notes**:
```csharp
// 5 separate tables (one-to-one with Order)
public class Order3DPrintingAttributes
{
    public string OrderId { get; set; } // PK + FK
    public bool ThreadTapRequired { get; set; }
    public bool InsertRequired { get; set; }
    public string? PartMarking { get; set; }
    public bool PartAssemblyTestRequired { get; set; }
}

public class OrderCncMachiningAttributes
{
    public string OrderId { get; set; }
    public bool TapRequired { get; set; }
    public string? Tolerance { get; set; } // "ISO 2768-1 ±0.125mm"
    public string? SurfaceRoughness { get; set; } // "3.2µm Ra"
    public string? InspectionType { get; set; } // "cmm-formal-report"
}

// EF Core configuration
modelBuilder.Entity<Order3DPrintingAttributes>()
    .HasKey(a => a.OrderId);

modelBuilder.Entity<Order3DPrintingAttributes>()
    .HasOne(a => a.Order)
    .WithOne(o => o.PrintingAttributes)
    .HasForeignKey<Order3DPrintingAttributes>(a => a.OrderId);

// Query examples
var tightToleranceOrders = await _context.OrderCncMachiningAttributes
    .Where(a => a.Tolerance == "ISO 2768-1 ±0.05mm")
    .Include(a => a.Order)
    .ToListAsync();

var complexDesignOrders = await _context.Order3DDesignAttributes
    .Where(a => a.ComplexityLevel == "Complex")
    .Select(a => a.Order)
    .ToListAsync();
```

---

## 13. File Role Classification with Simple 3-Value System

### Decision: Input/Output/Supporting Enum for File Categorization

**Rationale**:
- **Clear Distinction**: Input (customer-provided), Output (MALIEV-delivered), Supporting (other docs)
- **Manufacturing Filter**: Can filter "show only Input files for manufacturing" to identify CAD files to produce
- **Customer Downloads**: Filter "Output files" to show only deliverables (finished CAD, scan data, reports)
- **Simplicity**: 3 values easy to understand, no ambiguity (vs 10+ specific roles like "cad-input", "cad-output", "drawing", etc.)
- **Flexible**: Combine with file_category (CAD, Drawing, Image, Document) for more granular classification if needed

**Alternatives Considered**:
- **No File Role (Category Only)**: Rejected - Can't distinguish customer CAD vs MALIEV CAD, important for manufacturing workflows
- **Complex Role System (10+ values)**: Rejected - Over-engineering, "reference-sketch" vs "technical-sketch" adds no value, harder for users to select
- **Boolean Flags (isInput, isOutput)**: Rejected - Allows invalid states (both true, both false), less clear than enum

**Implementation Notes**:
```csharp
public enum FileRole
{
    Input,      // Files provided by customer (CAD files, sketches, reference images, drawings)
    Output,     // Files delivered by MALIEV (finished CAD models, 3D scan data, deviation reports)
    Supporting  // Other documentation (quotes, specifications, process notes, invoices)
}

public enum FileCategory
{
    CAD,        // STL, STEP, IGES, OBJ, SolidWorks files
    Drawing,    // 2D technical drawings (PDF, DWG, DXF)
    Image,      // Photos, reference images (JPG, PNG)
    Document,   // PDF documents, Word files, specifications
    Archive,    // ZIP, RAR, 7Z compressed files
    Other       // Miscellaneous files
}

public class OrderFile
{
    public long FileId { get; set; }
    public string OrderId { get; set; }
    public FileRole FileRole { get; set; }          // Input/Output/Supporting
    public FileCategory FileCategory { get; set; }   // CAD/Drawing/Image/Document/Archive
    public string? DesignUnits { get; set; }        // mm/inch/cm/m (nullable, CAD files only)
    // ... other fields
}

// Query examples
var inputCadFiles = await _context.OrderFiles
    .Where(f => f.OrderId == orderId &&
                f.FileRole == FileRole.Input &&
                f.FileCategory == FileCategory.CAD)
    .ToListAsync(); // Files to manufacture

var customerDeliverables = await _context.OrderFiles
    .Where(f => f.OrderId == orderId && f.FileRole == FileRole.Output)
    .ToListAsync(); // What MALIEV produced
```

**Benefits Analysis**:
- ✅ Enables manufacturing workflow: "Show me what to produce" (Input + CAD)
- ✅ Enables customer downloads: "Show me what I received" (Output)
- ✅ Reduces user confusion: 3 clear choices vs 10+ specific roles
- ✅ Maintains flexibility: Can add file_category for further classification

---

## 14. Order Notes System with Separate History Table

### Decision: Dedicated order_notes Table with Full Audit Trail

**Rationale**:
- **History Preservation**: Separate table maintains complete note history (who, when, what)
- **Two Note Types**: customer (visible to all) vs internal (employees only) for different use cases
- **Unbounded Notes**: No limit on number of notes per order (vs single text field with size limit)
- **Query Performance**: Can query "all orders with notes from specific employee", "all internal notes containing 'VIP'"
- **Access Control**: Easy to filter internal notes from customer API responses

**Alternatives Considered**:
- **Text Fields on Order Table**: Rejected - No history, single note only, can't track who/when added
- **Status Notes Only**: Rejected - Notes tied to status changes, can't add general coordination notes ("VIP customer", "material delayed")
- **JSONB Array**: Rejected - Hard to query, no type safety, complex updates

**Implementation Notes**:
```csharp
public class OrderNote
{
    public long NoteId { get; set; }               // Primary key
    public string OrderId { get; set; }            // Foreign key to Order
    public NoteType NoteType { get; set; }         // customer | internal
    public string NoteText { get; set; }           // Content
    public string CreatedBy { get; set; }          // UserId from UserContext
    public DateTime CreatedAt { get; set; }        // Timestamp
}

public enum NoteType
{
    Customer,   // Visible to customers and employees (requirements, preferences)
    Internal    // Visible only to employees (VIP status, delays, coordination)
}

// API endpoint for customers (filter internal notes)
public async Task<List<OrderNoteDto>> GetNotesForCustomerAsync(string orderId, UserContext userContext)
{
    var notes = await _context.OrderNotes
        .Where(n => n.OrderId == orderId && n.NoteType == NoteType.Customer)
        .OrderByDescending(n => n.CreatedAt)
        .ToListAsync();

    return _mapper.Map<List<OrderNoteDto>>(notes);
}

// API endpoint for employees (show all notes)
public async Task<List<OrderNoteDto>> GetNotesForEmployeeAsync(string orderId)
{
    var notes = await _context.OrderNotes
        .Where(n => n.OrderId == orderId)
        .OrderByDescending(n => n.CreatedAt)
        .ToListAsync();

    return _mapper.Map<List<OrderNoteDto>>(notes);
}

// Create note endpoint (RBAC enforced)
public async Task<OrderNoteDto> CreateNoteAsync(string orderId, CreateNoteRequest request, UserContext userContext)
{
    // Only employees can create internal notes
    if (request.NoteType == NoteType.Internal && userContext.UserType != "employee")
    {
        throw new ForbiddenException("Only employees can create internal notes");
    }

    var note = new OrderNote
    {
        OrderId = orderId,
        NoteType = request.NoteType,
        NoteText = request.NoteText,
        CreatedBy = userContext.UserId,
        CreatedAt = DateTime.UtcNow
    };

    await _context.OrderNotes.AddAsync(note);
    await _context.SaveChangesAsync();

    return _mapper.Map<OrderNoteDto>(note);
}
```

**Use Cases**:
- Customer adds note: "Please use black color for all parts" (customer type)
- Employee adds note: "VIP customer - rush order, prioritize" (internal type)
- Employee adds note: "Material supplier delayed, expect 3-day delay" (internal type)
- Customer sees only their own notes and customer-type notes
- Employees see all notes (customer + internal)

---

## 15. Design Units as Per-File Attribute

### Decision: design_units Column in OrderFile Table (Nullable)

**Rationale**:
- **Multi-Unit Orders**: One order can have files in different units (customer provides inch drawing, MALIEV delivers mm CAD)
- **File-Specific**: Units only relevant for CAD files, not applicable to images/documents/archives
- **Nullable**: Many files don't need units (photos, PDFs, supporting docs)
- **Correct Granularity**: Units belong to individual CAD file, not to entire order

**Alternatives Considered**:
- **design_units on Order Table**: Rejected - Assumes all files in order use same units, invalid for mixed-unit orders
- **Separate FileUnits Table**: Rejected - Over-normalization, 1:1 relationship better as nullable column
- **Hardcode to mm**: Rejected - International customers use inch/cm, must support multiple units

**Implementation Notes**:
```csharp
public class OrderFile
{
    public long FileId { get; set; }
    public string OrderId { get; set; }
    public FileRole FileRole { get; set; }
    public FileCategory FileCategory { get; set; }

    // Unit system for CAD files (nullable - only applies to CAD category)
    public string? DesignUnits { get; set; } // "mm" | "inch" | "cm" | "m"

    // Other fields...
    public string FileName { get; set; }
    public string ObjectPath { get; set; }
    public long FileSizeBytes { get; set; }
}

// FluentValidation rules
public class OrderFileValidator : AbstractValidator<OrderFile>
{
    public OrderFileValidator()
    {
        // design_units required only for CAD files
        When(f => f.FileCategory == FileCategory.CAD, () =>
        {
            RuleFor(f => f.DesignUnits)
                .NotEmpty()
                .Must(u => new[] { "mm", "inch", "cm", "m" }.Contains(u))
                .WithMessage("CAD files must specify design_units (mm/inch/cm/m)");
        });

        // design_units not allowed for non-CAD files
        When(f => f.FileCategory != FileCategory.CAD, () =>
        {
            RuleFor(f => f.DesignUnits)
                .Null()
                .WithMessage("design_units only applicable to CAD files");
        });
    }
}
```

**Example Scenarios**:
- Customer uploads `part_drawing.pdf` (inch dimensions) → design_units = "inch"
- Customer uploads `reference_photo.jpg` → design_units = null (not applicable)
- MALIEV delivers `finished_model.step` (converted to mm) → design_units = "mm"
- Order has 3 files: drawing (inch), CAD input (inch), CAD output (mm) → each file tracks its own units

---

## Technology Stack Summary

| Component | Technology | Version | Rationale |
|-----------|-----------|---------|-----------|
| Runtime | .NET | 9.0 | Latest LTS, performance improvements, native AOT support |
| Framework | ASP.NET Core | 9.0 | Microservice-optimized, built-in DI, middleware pipeline |
| ORM | Entity Framework Core | 9.0.9 | MALIEV standard, LINQ support, migration tooling |
| Database | PostgreSQL | 18 | MALIEV standard, JSONB support, robust concurrency |
| Database Provider | Npgsql | 9.0.2 | Official PostgreSQL provider for EF Core |
| Logging | Serilog | 8.0.2 | Structured JSON logging, stdout sink |
| Testing | xUnit | 2.9.2 | .NET standard, parallel test execution |
| Mocking | Moq | 4.20.72 | Simple mock syntax, MALIEV standard |
| Assertions | FluentAssertions | 8.6.0 | Readable assertions, detailed failure messages |
| Mapping | AutoMapper | 13.0.1 | DTO ↔ Entity mapping, reduces boilerplate |
| Validation | FluentValidation | 11.11.0 | Expressive validation rules, separates validation logic |
| Resilience | Polly | 8.4.2 | Retry, circuit breaker, timeout policies |
| API Docs | Swashbuckle | 7.2.0 | OpenAPI 3.0 generation, Swagger UI |
| Versioning | Asp.Versioning | 9.0.0 | URL-based API versioning |
| Health Checks | AspNetCore.HealthChecks | 9.0.0 | Liveness/readiness probes for Kubernetes |

---

## Configuration Management Strategy

**Environment Variables (from Google Secret Manager)**:

| Variable | Purpose | Example (Dev) |
|----------|---------|---------------|
| `ConnectionStrings__OrderDbContext` | PostgreSQL connection string | `Server=localhost;Port=5432;Database=order_app_db;...` |
| `CUSTOMER_SERVICE_URL` | Customer Service base URL | `https://dev.api.maliev.com/customers` |
| `MATERIAL_SERVICE_URL` | Material Service base URL | `https://dev.api.maliev.com/materials` |
| `PAYMENT_SERVICE_URL` | Payment Service base URL | `https://dev.api.maliev.com/payments` |
| `UPLOAD_SERVICE_URL` | Upload Service base URL | `https://dev.api.maliev.com/uploads` |
| `AUTH_SERVICE_URL` | Auth Service base URL | `https://dev.api.maliev.com/auth` |
| `EMPLOYEE_SERVICE_URL` | Employee Service base URL | `https://dev.api.maliev.com/employees` |
| `NOTIFICATION_SERVICE_URL` | Notification Service base URL | `https://dev.api.maliev.com/notifications` |
| `CORS_ALLOWED_ORIGINS` | Comma-separated allowed origins | `https://dev.intranet.maliev.com,https://dev.www.maliev.com` |

**Secrets Management Compliance**:
- ✅ No secrets in `appsettings.json` (uses `${ENV_VAR}` placeholders)
- ✅ No production URLs in source code (environment-based resolution)
- ✅ Documentation uses `https://example.com` for examples
- ✅ Local development uses `localhost` fallbacks

---

## Performance Optimization Strategy

1. **Database Query Optimization**
   - Use `AsNoTracking()` for read-only queries (status history, file list)
   - Index frequently queried columns: `CustomerId`, `Status`, `ServiceCategoryId`
   - Batch load related data with `Include()` to avoid N+1 queries

2. **Caching Strategy**
   - Cache service categories and process types (rarely change, high read volume)
   - Cache customer NDA status for 5 minutes (reduce Customer Service calls)
   - Use `IMemoryCache` for in-process caching (simple, no Redis dependency initially)

3. **Connection Pooling**
   - `IHttpClientFactory` for connection reuse across service calls
   - PostgreSQL connection pooling (Npgsql default: 100 max connections)

4. **Async All the Way**
   - All I/O operations use `async/await` (database, HTTP calls)
   - Avoid `Task.Result` or `.Wait()` to prevent thread pool starvation

---

## Security Considerations

1. **Data Encryption**
   - Confidential order notes encrypted at rest (EF Core column-level encryption)
   - TLS 1.2+ for all HTTP communication (inter-service calls)

2. **Input Validation**
   - FluentValidation for all request DTOs (file size limits, format checks)
   - Sanitize file names to prevent path traversal attacks

3. **RBAC Enforcement**
   - Middleware extracts `UserContext` from Auth Service
   - Controllers check `UserType` and `Roles` before data access
   - Example: `if (userContext.UserType == "customer" && order.CustomerId != userContext.UserId) return Forbid();`

4. **Audit Trail**
   - All sensitive operations logged to `AuditLog` table
   - Include `PerformedBy` (userId) for accountability

---

## Dependency Injection Structure

```csharp
// Program.cs service registration order
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("OrderDbContext")));

// AutoMapper profiles
builder.Services.AddAutoMapper(typeof(MappingProfile));

// FluentValidation validators
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderRequestValidator>();

// Business services
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IFileService, FileService>();

// HTTP clients for external services (7 total)
builder.Services.AddHttpClient("CustomerService", client => /* config */);
builder.Services.AddHttpClient("MaterialService", client => /* config */);
builder.Services.AddHttpClient("PaymentService", client => /* config */);
builder.Services.AddHttpClient("UploadService", client => /* config */)
    .AddTransientHttpErrorPolicy(/* Polly retry */);
builder.Services.AddHttpClient("AuthService", client => /* config */);
builder.Services.AddHttpClient("EmployeeService", client => /* config */);
builder.Services.AddHttpClient("NotificationService", client => /* config */);

// Service clients (typed wrappers)
builder.Services.AddScoped<ICustomerServiceClient, CustomerServiceClient>();
builder.Services.AddScoped<IMaterialServiceClient, MaterialServiceClient>();
builder.Services.AddScoped<IPaymentServiceClient, PaymentServiceClient>();
builder.Services.AddScoped<IFileServiceClient, FileServiceClient>();
builder.Services.AddScoped<IAuthServiceClient, AuthServiceClient>();
builder.Services.AddScoped<IEmployeeServiceClient, EmployeeServiceClient>();
builder.Services.AddScoped<INotificationServiceClient, NotificationServiceClient>();

// Background services
builder.Services.AddHostedService<FileCleanupService>();

// Memory cache
builder.Services.AddMemoryCache();

// CORS
builder.Services.AddCors(/* environment-based config */);

// Health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("OrderDbContext"))
    .AddUrlGroup(new Uri($"{Environment.GetEnvironmentVariable("CUSTOMER_SERVICE_URL")}/health"), "CustomerService");
```

---

## Next Steps (Phase 1)

With all technical decisions documented, Phase 1 will:

1. **Create `data-model.md`**: Detailed entity definitions, relationships, validation rules
2. **Generate OpenAPI contracts**: `contracts/orders.yaml`, `contracts/files.yaml`, `contracts/statuses.yaml`, `contracts/webhooks.yaml`
3. **Write failing contract tests**: One test per endpoint to validate schema compliance
4. **Document quickstart scenarios**: 6 end-to-end integration test scenarios from spec
5. **Update `CLAUDE.md`**: Add Order Service-specific patterns, state machine, external service endpoints

All implementation will follow TDD: Tests first (fail) → Implementation (pass) → Refactor.

---

*Research complete. Ready for Phase 1: Design & Contracts.*
