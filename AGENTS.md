# AGENTS.md

This file provides context, commands, and strict guidelines for AI agents working on the `Maliev.OrderService` repository.
**Strictly adhere to these instructions.**

## 1. Project Overview

- **Framework**: .NET 10.0 (ASP.NET Core Web API)
- **Database**: PostgreSQL (Npgsql, EF Core 10.0.0). Uses `xmin` system column for optimistic concurrency.
- **Messaging**: MassTransit (RabbitMQ) with `Maliev.MessagingContracts`.
- **Integration**: Aspire Service Defaults, Google Secret Manager (`/mnt/secrets`).
- **Architecture**: Clean Architecture (Api, Application, Domain, Infrastructure, Tests)
- **Structure**:
    - `Maliev.OrderService.Api`: Controllers, Middleware
    - `Maliev.OrderService.Application`: Use cases, handlers, DTOs
    - `Maliev.OrderService.Domain`: Entities, interfaces
    - `Maliev.OrderService.Infrastructure`: EF Core, repositories
    - `Maliev.OrderService.Tests`: xUnit, Testcontainers, Integration Tests

---

## 2. Build, Test & Lint Commands

All commands run from within this service directory (`B:\maliev\Maliev.OrderService`).

### Build
```powershell
dotnet build Maliev.OrderService.slnx
```

### Run Tests
```powershell
# Run all tests
dotnet test Maliev.OrderService.slnx --verbosity normal

# Run a single test method
dotnet test --filter "FullyQualifiedName~OrdersControllerTests.GetOrders_ReturnsOk"

# Run all tests in a class
dotnet test --filter "FullyQualifiedName~OrdersControllerTests"

# Run with code coverage
dotnet test Maliev.OrderService.slnx --collect:"XPlat Code Coverage"
```

### Format Check
```powershell
dotnet format Maliev.OrderService.slnx
```

### EF Core Migrations (Infrastructure project only)
```powershell
dotnet ef migrations add <Name> --project Maliev.OrderService.Infrastructure --startup-project Maliev.OrderService.Infrastructure
```

### Run Application
```bash
dotnet run --project Maliev.OrderService.Api/Maliev.OrderService.Api.csproj
```

---

## 3. Code Style & Conventions

### Workspace Structure
```
Maliev.OrderService/
├── Maliev.OrderService.Api/           # Controllers, Consumers, Middleware
├── Maliev.OrderService.Application/   # Use cases, DTOs, Interfaces, Handlers
├── Maliev.OrderService.Domain/        # Entities, value objects, domain interfaces
├── Maliev.OrderService.Infrastructure/ # EF Core DbContext, repositories, HTTP clients
├── Maliev.OrderService.Tests/         # Unit + Integration tests (xUnit)
├── Directory.Build.props              # Central package versioning
└── Maliev.OrderService.slnx          # Solution file (.slnx preferred over .sln)
```

### C# Naming & Formatting
- **Namespaces**: File-scoped (`namespace Maliev.OrderService.Domain.Entities;`)
- **Classes/Methods/Properties**: `PascalCase`
- **Private fields**: `_camelCase` (underscore prefix)
- **Parameters/locals**: `camelCase`
- **Async methods**: Suffix with `Async` (e.g., `GetOrderAsync`)
- **Interfaces**: Prefix with `I` (e.g., `IOrderService`)
- **Permissions**: GCP-style `{domain}.{plural-resource}.{action}` as `public const string` in a `Permissions` static class
  - Valid: `order.orders.create`, `order.orders.update`
  - Invalid: `order.order.create` (singular), `order.create` (missing resource)
- **XML docs**: Required on ALL public methods and properties
- **Nullable**: Enabled (`<Nullable>enable</Nullable>`). Use `?` explicitly
- **Imports**: System first, then third-party, then local. Alphabetize within groups. Remove unused `using`
- **Braces**: Allman style (new line) for methods and control structures. Expression-bodied for properties/accessors
- **Indentation**: 4 spaces, LF line endings, UTF-8, trim trailing whitespace

### C# Patterns
- **DI**: Constructor injection with `private readonly` fields. Primary constructors are also used in this service:
    ```csharp
    public partial class OrderService(OrderDbContext context, ILogger<OrderService> logger) : IOrderService { ... }
    ```
- **Controllers**: `[ApiController]`, `[ApiVersion("1")]`, `[Route("order/v{version:apiVersion}")]`
- **Logging**: Source-generated logging is used in this service:
    ```csharp
    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Order {OrderId} created")]
        public static partial void OrderCreated(ILogger logger, string orderId);
    }
    // Usage: Log.OrderCreated(_logger, orderId);
    ```
- **Error handling**: Global exception middleware. Return `ProblemDetails` / `ErrorResponse` DTOs. Never expose stack traces
- **JSON**: Check existing conventions in this service for naming policy
- **Manual mapping**: Static extension methods (`ToDto()`, `ToEntity()`). AutoMapper is banned
- **Validation**: `System.ComponentModel.DataAnnotations` on DTOs. FluentValidation is banned
- **Collection Expressions**: Use `[]` for arrays/lists.
    ```csharp
    List<string> items = ["a", "b"];
    return [.. existingItems, newItem];
    ```
- **Async/Await**: Always pass `CancellationToken` as the last optional parameter.

---

## 4. Architecture & Patterns

### Controllers & Services
- **Thin Controllers**: Delegate logic to Scoped Business Services (e.g., `IOrderManagementService`).
- **DTOs**: STRICT separation between Entities and DTOs. Never return Entity classes directly.
- **Mapping**: Use manual mapping extensions (e.g., `order.ToOrderResponse()`).

### Authorization (RBAC & Data Isolation)
- **Attributes**: Use `[RequirePermission(OrderPermissions.X)]` on endpoints.
- **Data Isolation**: Use `IOrderAuthorizationService.ApplyDataIsolationFilter(User, query)` to restrict data access based on UserType (Customer/Employee/Manager).
- **Object routes**: Any route with `{orderId}` must load the order and enforce `IOrderAuthorizationService.CanViewOrder(User, order)` before returning or mutating statuses, files, notes, preview images, outsourcing data, batch updates, or cancellations.
- **Batch operations**: Validate every target order's object access before starting the transaction; do not partially apply authorized items when another item is forbidden.
- **Claims**: User context is derived from JWT claims (`uid`, `role`, `userType`).

### Database & Persistence
- **Optimistic Concurrency**: Use `xmin` shadow property.
    ```csharp
    // Reading
    var xmin = _context.Entry(entity).Property<uint>("xmin").CurrentValue;
    // Writing/Checking
    if (originalXmin != currentXmin) throw new DbUpdateConcurrencyException(...);
    ```
- **Transactions**: Use `BeginTransactionAsync` for multi-step operations.

### Configuration & Secrets
- **Pattern**: Inject `IConfiguration`. **NEVER** hardcode secrets/URLs.
- **Secret Manager**: Secrets are mounted at `/mnt/secrets`.
- **Naming**: `Jwt__SecurityKey` in secrets becomes `Jwt:SecurityKey` in config.
- **External Services**: Use `ExternalServiceOptions` pattern (BaseUrl, Timeout).

---

## 5. Banned Libraries (Build Will Fail)

| Banned | Use Instead |
|--------|-------------|
| AutoMapper | Manual mapping extensions |
| FluentValidation | DataAnnotations or manual validation |
| FluentAssertions | Standard xUnit `Assert.*` |
| Swashbuckle/Swagger | Scalar (at `/order/scalar`) |
| InMemoryDatabase (EF Core) | Testcontainers with real PostgreSQL |

---

## 6. Testing Rules

- **Framework**: xUnit with standard `Assert` (`Assert.Equal`, `Assert.NotNull`, etc.)
- **Naming**: `MethodName_StateUnderTest_ExpectedBehavior` or `HTTP_METHOD_Path_Scenario_ExpectedStatus`
- **Coverage**: Minimum 80% per service
- **Integration tests**: `BaseIntegrationTestFactory<TProgram, TDbContext>` with Testcontainers (PostgreSQL, Redis, RabbitMQ). Never InMemoryDatabase
- **System tests** (Tier 3): `AspireTestFixture` with `[Collection("AspireDomainTests")]` — shared AppHost, never one per class
- **Eventual consistency**: Use `TestHelpers.WaitForAsync`. Never `Task.Delay`
- **MassTransit consumers**: Must have consumer tests using `AddMassTransitTestHarness()`

### Testing Strategy (4-Tier Pyramid Context)

This service's tests cover **Tier 1 (Unit)** and **Tier 2 (Service Integration)** of the Maliev testing pyramid:

| Tier | What to Test | Infrastructure |
|------|-------------|---------------|
| **Unit** | Business logic, domain models, service methods with mocked dependencies | None (mocks only) |
| **Service Integration** | API endpoints, database persistence, permission enforcement, input validation | `BaseIntegrationTestFactory` + Testcontainers (Postgres/Redis/RabbitMQ) |

**Tier 3 (System Integration)** — cross-service workflows and event chains — is tested in `Maliev.Aspire.Tests/`.

> Full ecosystem test strategy: `Maliev.Aspire.Tests/TEST_PLAN.md`

---

## 7. Mandatory Rules

- **`TreatWarningsAsErrors = true`**: Zero warnings allowed. No suppression
- **`[RequirePermission("order.resources.action")]`**: On all endpoints, not plain `[Authorize]`
- **API versioning**: All routes versioned (`v1/`)
- **Service prefix**: Routes prefixed with `/order`
- **Scalar docs**: Configured at `/order/scalar`
- **Secrets**: Never hardcoded. Use GCP Secret Manager or environment variables
- **Async/await**: All the way down. Pass `CancellationToken`
- **EF Core Design package**: Only in Infrastructure project, never in Api
- **PostgreSQL xmin**: Shadow property only — `entity.Property<uint>("xmin").HasColumnType("xid").IsRowVersion()`. Never add entity property
- **Temporary files**: Generate in `/temp` folder, clean up afterwards

---

## 8. Git Rules

- Each `Maliev.*` folder is an independent git repo. `cd` into it before git commands
- **Commit early and often** after every meaningful unit of work. Do not accumulate changes
- **Never use `git checkout` to restore files** — commit first, then `git revert` or `git reset --soft`
- Feature branches merged to `develop` via PR. Do not push without being asked
