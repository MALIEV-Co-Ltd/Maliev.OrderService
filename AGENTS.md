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

## 2. Build & Test Commands

Always verify changes before submitting.

### Build
```bash
dotnet build Maliev.OrderService.Api/Maliev.OrderService.Api.csproj
```

### Run Tests
**Run all tests (Integration & Unit):**
```bash
dotnet test Maliev.OrderService.Tests/Maliev.OrderService.Tests.csproj
```

**Run a single test (CRITICAL for iteration):**
Use `--filter` with the fully qualified name.
```bash
dotnet test Maliev.OrderService.Tests/Maliev.OrderService.Tests.csproj --filter "FullyQualifiedName~Maliev.OrderService.Tests.Controllers.OrdersControllerTests.GetOrders_ReturnsOk"
```

### Run Application
```bash
dotnet run --project Maliev.OrderService.Api/Maliev.OrderService.Api.csproj
```

## 3. Code Style & Conventions

### General
- **Nullable**: `<Nullable>enable</Nullable>` is on. Handle nulls explicitly.
- **Warnings**: Treat warnings as errors. No unused variables or missing awaits.
- **Implicit Usings**: Enabled. Do not add `using System;` etc. explicitly if covered.
- **File-Scoped Namespaces**: Use `namespace Maliev.OrderService.Api.Services;` (no braces).

### Modern C# Features (Mandatory)
- **Primary Constructors**: Use for DI.
    ```csharp
    public partial class OrderService(OrderDbContext context, ILogger<OrderService> logger) : IOrderService { ... }
    ```
- **Async/Await**: Always pass `CancellationToken` as the last optional parameter.
    ```csharp
    public async Task<Order?> GetAsync(string id, CancellationToken cancellationToken = default) { ... }
    ```
- **Collection Expressions**: Use `[]` for arrays/lists.
    ```csharp
    List<string> items = ["a", "b"];
    return [.. existingItems, newItem];
    ```

### Logging
- **Source Generated Logging**: **DO NOT** use `logger.LogInformation(...)`.
- Create a `private static partial class Log` inside the class.
    ```csharp
    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Order {OrderId} created")]
        public static partial void OrderCreated(ILogger logger, string orderId);
    }
    // Usage: Log.OrderCreated(_logger, orderId);
    ```

### Error Handling & Results
- **Controllers**: Return `IActionResult` (`Ok`, `NotFound`, `BadRequest`, `Conflict`, `Forbid`).
- **DTOs**: Use `ErrorMessageResponse` for failures.
- **Concurrency**: Catch `DbUpdateConcurrencyException` -> return HTTP 409 Conflict.
- **Validation**: Use Data Annotations (`[Required]`, `[EmailAddress]`) or `ModelState.IsValid`.

## 4. Architecture & Patterns

### Controllers & Services
- **Thin Controllers**: Delegate logic to Scoped Business Services (e.g., `IOrderManagementService`).
- **DTOs**: STRICT separation between Entities and DTOs. Never return Entity classes directly.
- **Mapping**: Use manual mapping extensions (e.g., `order.ToOrderResponse()`).

### Authorization (RBAC & Data Isolation)
- **Attributes**: Use `[RequirePermission(OrderPermissions.X)]` on endpoints.
- **Data Isolation**: Use `IOrderAuthorizationService.ApplyDataIsolationFilter(User, query)` to restrict data access based on UserType (Customer/Employee/Manager).
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

### Testing Strategy
- **Integration First**: Prefer integration tests using `Testcontainers` (Postgres, RabbitMQ).
- **Configuration**: `TestDatabaseFixture` builds config from `appsettings.Testing.json` -> User Secrets -> Env Vars.
- **Mocking**: Use `Moq` sparingly, primarily for external HTTP services or unit tests.

### Testing Strategy (4-Tier Pyramid Context)

This service's tests cover **Tier 1 (Unit)** and **Tier 2 (Service Integration)** of the Maliev testing pyramid:

| Tier | What to Test | Infrastructure |
|------|-------------|---------------|
| **Unit** | Business logic, domain models, service methods with mocked dependencies | None (mocks only) |
| **Service Integration** | API endpoints, database persistence, permission enforcement, input validation | `BaseIntegrationTestFactory` + Testcontainers (Postgres/Redis/RabbitMQ) |

**Tier 3 (System Integration)** — cross-service workflows and event chains — is tested in `Maliev.Aspire.Tests/`.

#### Key Rules
- Use `BaseIntegrationTestFactory<TProgram, TDbContext>` for integration tests (real Testcontainers, never InMemoryDatabase)
- Every MassTransit consumer MUST have a consumer test using `services.AddMassTransitTestHarness()`
- Test naming: `MethodName_StateUnderTest_ExpectedBehavior`
- Minimum 80% code coverage
- Use `[Fact]` for single cases, `[Theory]` for parameterized tests

> Full ecosystem test strategy: `Maliev.Aspire.Tests/TEST_PLAN.md`

## 5. Security & Safety
- **Secrets**: Never commit `.env` or secrets.
- **Validation**: Validate all inputs.
- **Crypto**: Use platform-provided crypto.

---

## Git & Version Control — Mandatory Rules

### 🚨 CRITICAL: Always Commit Code Changes (Non-Negotiable)
- **You MUST commit your changes to the local repository after completing any meaningful unit of work.**
- **Never accumulate uncommitted changes.** Do not wait until end of session or until something breaks.
- **Commit early and often** — if a change is meaningful (even a small fix or refactor), commit it.
- **You do NOT need to push to remote** — local commits are sufficient to protect against accidental loss.
- **If you are unsure whether to commit, commit anyway.** Extra commits are harmless; lost work is irreversible.
- This rule applies even if you are just "testing" or "exploring" — use git branches to isolate experimental work and commit those changes too.

### 🚨 CRITICAL: Never Use `git checkout` to Restore Broken Files
- **NEVER use `git checkout` to restore or recover files.** This operation discards uncommitted changes permanently and will result in data loss.
- **To undo/recover from broken files: first commit your current changes, then use `git revert` or `git reset --soft` to safely undo.**
