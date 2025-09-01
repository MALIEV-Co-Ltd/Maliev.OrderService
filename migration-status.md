# Migration Plan for Maliev.OrderService

This document outlines the step-by-step plan for migrating the `Maliev.OrderService` to a modern, multi-project, production-ready .NET 9 solution.

**Part 1: Triage and Mode Selection**
*   Initial Migration (Confirmed)

**Part 2: Mandatory Execution Plan**

### Step 1: Plan and Dynamic Discovery
*   Scanned `migration_source` and identified `.csproj` files and dependencies.
    *   `Maliev.OrderService.Api` (Web Project) -> Depends on `Maliev.OrderService.Common`, `Maliev.OrderService.Data`
    *   `Maliev.OrderService.Common` (Class Library)
    *   `Maliev.OrderService.Data` (Class Library, Data Access)
    *   `Maliev.OrderService.Tests` (Test Project) -> Depends on `Maliev.OrderService.Api`
*   Created `migration-status.md` (This file).
*   Updated `.gitignore` to exclude `migration_source/` and `reference_project/`. (Pending)

### Step 2: Create and Clean Project Skeletons
For each discovered project, create the new .NET 9 project, delete all boilerplate files, and add it to the solution.
*   Create `Maliev.OrderService.Api` (`net9.0`, `Microsoft.NET.Sdk.Web`)
*   Create `Maliev.OrderService.Common` (`net9.0`, `Microsoft.NET.Sdk`)
*   Create `Maliev.OrderService.Data` (`net9.0`, `Microsoft.NET.Sdk`)
*   Create `Maliev.OrderService.Tests` (`net9.0`, `Microsoft.NET.Sdk`)
*   Add all new projects to `Maliev.OrderService.sln`.
*   Delete boilerplate files from each new project.

### Step 3: Establish Project References
Based on the dependency graph, add the correct `<ProjectReference>` tags to all new `.csproj` files.
*   `Maliev.OrderService.Api` references `Maliev.OrderService.Common` and `Maliev.OrderService.Data`.
*   `Maliev.OrderService.Tests` references `Maliev.OrderService.Api`.

### Step 4: Re-implement Supporting Libraries
Analyze the source of supporting libraries (e.g., `.Common`) and write new, modernized code that replicates their functionality.
*   Re-implement `Maliev.OrderService.Common` content.

### Step 5: Implement Core Functionality and Replicate `Program.cs`
*   **5.1 - Code Generation**
    *   Analyze `migration_source/Maliev.OrderService.Data/Data/OrderContext.cs` for `OnModelCreating` to determine schema.
    *   Generate new, modern C# 9 code for:
        *   Entities (in `Maliev.OrderService.Data/Models`)
        *   DTOs (in `Maliev.OrderService.Api/Models/DTOs`)
        *   `IOrderServiceService` (in `Maliev.OrderService.Api/Services`)
        *   `OrderServiceService` (in `Maliev.OrderService.Api/Services`)
        *   "Thin" Controllers (in `Maliev.OrderService.Api/Controllers`)
*   **5.2 - Replicate `Program.cs` from the Reference Project**
    *   Analyze `reference_project/Maliev.JobService/Maliev.JobService.Api/Program.cs` (or `Maliev.CountryService`) as the blueprint.
    *   Replicate `Program.cs` in `Maliev.OrderService.Api` including:
        *   Service Registration Order
        *   Authentication (`AddAuthentication()`, `AddJwtBearer()`)
        *   API Versioning (`AddApiVersioning()`, `AddApiExplorer()`)
        *   Swagger Configuration (versioned endpoints, `OpenApiInfo`, `OpenApiSecurityScheme`)
        *   CORS Policy (`*.maliev.com` origins)
        *   Exception Handling (`UseExceptionHandler`)
        *   Middleware Pipeline Order (`UsePathBase`, `UseHttpsRedirection`, `UseCors`, `UseAuthentication`, `UseAuthorization`, `UseSwagger`, `UseSwaggerUI`)

### Step 6: Write Comprehensive Unit Tests
*   **6.1 - Service Layer Tests**
    *   Create unit tests for each public method in `OrderServiceService` using a mocking framework (Moq).
    *   Cover success, failure, and edge cases.
*   **6.2 - Controller Tests**
    *   Create unit tests for each controller action, mocking `IOrderServiceService`.
    *   Verify correct `IActionResult` responses.

### Step 7: Configure Local Secrets
*   Execute `dotnet user-secrets set` commands for `Jwt:Issuer`, `Jwt:Audience`, `JwtSecurityKey`, and `ConnectionStrings:OrderServiceDbContext`.

### Step 8: Final Verification
*   Build the Solution (`dotnet build`).
*   Run All Tests (`dotnet test`).

### Step 9: API Standardization and Documentation
*   Standardize API routes.
*   Generate `GEMINI.md`.
*   Update `README.md`.
*   Present `ACTION REQUIRED` block with `gcloud secrets` commands.