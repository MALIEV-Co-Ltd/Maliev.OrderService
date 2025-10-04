# Implementation Plan: Order Service API for Rapid Prototyping & Manufacturing

**Branch**: `001-create-a-comprehensive` | **Date**: 2025-10-02 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-create-a-comprehensive/spec.md`

## Execution Flow (/plan command scope)
```
1. Load feature spec from Input path ✅
2. Fill Technical Context ✅
3. Fill Constitution Check section ✅
4. Evaluate Constitution Check → Execute Phase 0
5. Execute Phase 0 → research.md
6. Execute Phase 1 → contracts, data-model.md, quickstart.md, CLAUDE.md
7. Re-evaluate Constitution Check
8. Plan Phase 2 → Describe task generation approach
9. STOP - Ready for /tasks command
```

## Summary

Comprehensive Order Service API for MALIEV's rapid prototyping and manufacturing business. The service manages the complete order lifecycle from creation through fulfillment for diverse manufacturing services (3D printing, CNC machining, scanning, design services, etc.). Key capabilities include:

- **Order Management**: Multi-service order creation, 16-state workflow, batch operations, optimistic concurrency, quantity tracking, material selection
- **File Handling**: Multi-format uploads (CAD, images, docs), 100MB/file limit, automatic retry, GCS integration, file role classification (Input/Output/Supporting)
- **Access Control**: RBAC with Customer/Employee/Manager/Admin roles, context-based authorization via Auth Service
- **Confidentiality**: Automatic NDA-based order protection via Customer Service integration
- **Notes System**: Order notes with full history (customer-visible and internal employee notes)
- **Integration**: Material Service (catalog), Payment Service, Notification Service, Employee Service, Upload Service, Customer Service
- **Compliance**: 7-year data retention, 30-day file deletion, audit logging, GDPR compliance

**Technical Approach**: .NET 9 WebAPI microservice with PostgreSQL database (13 tables), EF Core ORM, JWT authentication, environment-based configuration for all service endpoints, containerized deployment via Kubernetes/ArgoCD.

## Technical Context

**Language/Version**: C# / .NET 9.0
**Primary Dependencies**: ASP.NET Core 9.0, Entity Framework Core 9.0.9, Npgsql 9.0.2, Serilog 8.0.2, AutoMapper, FluentValidation
**Storage**: PostgreSQL 18 (`order_app_db` database)
**Testing**: xUnit, Moq 4.20.72, FluentAssertions 8.6.0
**Target Platform**: Linux containers (Kubernetes/GKE)
**Project Type**: Microservice (.NET WebAPI)
**Performance Goals**: <200ms p95 response time, 500+ req/s throughput, <512MB memory per pod
**Constraints**: Stateless service, optimistic concurrency, environment-based service discovery, JWT-only auth
**Scale/Scope**: 10K+ orders/month, 63 functional requirements, 7 external service integrations (Customer, Material, Payment, Upload, Auth, Employee, Notification), 16-state workflow, 13 database tables

## Constitution Check
*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**I. Service Autonomy** (NON-NEGOTIABLE)
- [x] Service owns its database and schema (no shared DB access) - Dedicated `order_app_db` PostgreSQL database
- [x] Domain logic independent of other services - Order workflow logic self-contained
- [x] Communication only via stable APIs or events - HTTP/REST APIs for all integrations
- [x] No direct database access to other services - Uses API calls to Customer/Payment/Employee/Upload/Notification services

**II. Explicit Contracts**
- [x] All API endpoints have OpenAPI/Swagger documentation - Swagger UI at `/orders/swagger`
- [x] Data contracts are versioned (MAJOR.MINOR) - API versioning via Asp.Versioning
- [x] Schema changes include backward-compatible migration plan - EF Core migrations with rollback strategy

**III. Test-First Development** (NON-NEGOTIABLE)
- [x] Tests written and approved before implementation - Contract tests in Phase 1, implementation in Phase 4
- [x] Red-Green-Refactor cycle enforced - Tests fail until implementation complete
- [x] 80% minimum coverage for business-critical logic - Order workflow, RBAC, state transitions, batch operations
- [x] Integration tests for inter-service interactions - Customer Service, Payment Service, Upload Service integration tests

**IV. Auditability & Observability**
- [x] Structured logging (JSON, stdout) with traceable user/action info - Serilog JSON console output
- [x] Health checks exposed (liveness/readiness) - `/orders/liveness`, `/orders/readiness`
- [x] Audit logs tamper-proof and retention-compliant - Audit table with 7-year retention, append-only pattern

**V. Security & Compliance**
- [x] JWT authentication for service endpoints - MALIEV Auth Service integration
- [x] Role-based authorization enforced - Customer/Employee/Manager/Admin roles with context-based authorization
- [x] No sensitive data stored unencrypted - EF Core encrypted columns for confidential notes
- [x] Regulatory compliance addressed (GDPR, Thai taxation, etc.) - 7-year retention, GDPR right-to-erasure support

**VI. Secrets Management** (NON-NEGOTIABLE)
- [x] No secrets in source code (connection strings, API endpoints, keys) - All config via environment variables
- [x] All sensitive config via environment variables from Google Secret Manager - `ConnectionStrings__OrderDbContext`, `CUSTOMER_SERVICE_URL`, `MATERIAL_SERVICE_URL`, `PAYMENT_SERVICE_URL`, `UPLOAD_SERVICE_URL`, `AUTH_SERVICE_URL`, `EMPLOYEE_SERVICE_URL`, `NOTIFICATION_SERVICE_URL`
- [x] Source code uses only placeholders (`<secret-value>`, `${ENV_VAR}`) - Example: `services.AddHttpClient("MaterialService", client => client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("MATERIAL_SERVICE_URL") ?? "https://example.com"));`
- [x] Documentation uses example domains only - `https://example.com/api` in code comments
- [x] No production URLs or infrastructure topology exposed - Environment-specific config only

**VII. Zero Warnings Policy** (NON-NEGOTIABLE)
- [x] Build produces zero warnings and zero errors - `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` in .csproj
- [x] Code analysis rules enforced without suppressions - .editorconfig with strict rules
- [x] CI/CD treats warnings as build failures - GitHub Actions workflow validation

**VIII. Clean Project Artifacts** (NON-NEGOTIABLE)
- [x] No unused boilerplate or sample files - Remove default WeatherForecast controller/model
- [x] No outdated documentation or configuration - Only spec-driven documentation
- [x] Git ignore patterns exclude all generated/temporary files - .gitignore includes bin/, obj/, .vs/, *.user

**IX. Simplicity & Maintainability**
- [x] YAGNI principle applied (build only what's required) - No speculative features, spec-driven only
- [x] Stateless services preferred (no global state) - Stateless WebAPI, state in PostgreSQL only
- [x] Clear, readable code over clever optimizations - Standard layered architecture, no magic
- [x] Shared libraries are documented, tested, and versioned - No shared libraries in Phase 1

## Project Structure

### Documentation (this feature)
```
specs/001-create-a-comprehensive/
├── spec.md              # Feature specification (completed)
├── plan.md              # This file (/plan command output)
├── research.md          # Phase 0 output (/plan command)
├── data-model.md        # Phase 1 output (/plan command)
├── quickstart.md        # Phase 1 output (/plan command)
├── contracts/           # Phase 1 output (/plan command)
│   └── openapi.yaml     # Full OpenAPI 3.0 specification (all endpoints)
└── tasks.md             # Phase 2 output (/tasks command - NOT created by /plan)
```

### Source Code (repository root)
```
Maliev.OrderService/
├── Maliev.OrderService.Api/          # WebAPI project
│   ├── Controllers/
│   │   ├── OrdersController.cs       # Order CRUD, batch operations
│   │   ├── OrderStatusController.cs  # Status updates, history
│   │   ├── OrderFilesController.cs   # File upload/download/delete
│   │   └── OrderNotesController.cs   # Order notes management
│   ├── Models/
│   │   ├── DTOs/                     # Request/response DTOs
│   │   ├── Requests/                 # API request models
│   │   └── Responses/                # API response models
│   ├── Services/
│   │   ├── IOrderService.cs          # Order business logic interface
│   │   ├── OrderService.cs           # Order business logic implementation
│   │   ├── IFileService.cs           # File management interface
│   │   ├── FileService.cs            # Upload Service integration
│   │   ├── ICustomerService.cs       # Customer Service client interface
│   │   ├── CustomerServiceClient.cs  # Customer Service HTTP client
│   │   ├── IMaterialService.cs       # Material Service client interface
│   │   ├── MaterialServiceClient.cs  # Material Service HTTP client
│   │   ├── IPaymentService.cs        # Payment Service client interface
│   │   ├── PaymentServiceClient.cs   # Payment Service HTTP client
│   │   ├── INotificationService.cs   # Notification Service client interface
│   │   ├── NotificationServiceClient.cs # Notification Service HTTP client
│   │   ├── IEmployeeService.cs       # Employee Service client interface
│   │   └── EmployeeServiceClient.cs  # Employee Service HTTP client
│   ├── Middleware/
│   │   ├── JwtAuthenticationMiddleware.cs  # Auth Service integration
│   │   └── ExceptionHandlingMiddleware.cs  # Global error handling
│   ├── Profiles/
│   │   └── MappingProfile.cs         # AutoMapper configuration
│   ├── Validators/
│   │   ├── CreateOrderRequestValidator.cs
│   │   ├── UpdateOrderRequestValidator.cs
│   │   └── CreateFileRequestValidator.cs
│   ├── Program.cs                    # Application entry point
│   ├── appsettings.json              # Non-sensitive config only
│   ├── appsettings.Development.json  # Local dev overrides
│   └── Maliev.OrderService.Api.csproj
│
├── Maliev.OrderService.Data/         # Data layer project
│   ├── OrderDbContext.cs             # EF Core DbContext
│   ├── Models/
│   │   ├── Order.cs                  # Order entity (with material, quantity, date fields)
│   │   ├── OrderStatus.cs            # Order status entity
│   │   ├── OrderFile.cs              # File metadata entity (with file_role, file_category, design_units)
│   │   ├── OrderNote.cs              # Order notes entity (customer/internal)
│   │   ├── Order3DPrintingAttributes.cs    # 3D printing-specific attributes
│   │   ├── OrderCncMachiningAttributes.cs  # CNC machining-specific attributes
│   │   ├── OrderSheetMetalAttributes.cs    # Sheet metal-specific attributes
│   │   ├── Order3DScanningAttributes.cs    # 3D scanning-specific attributes
│   │   ├── Order3DDesignAttributes.cs      # 3D design-specific attributes
│   │   ├── ServiceCategory.cs        # Service category entity
│   │   ├── ProcessType.cs            # Process type entity
│   │   ├── AuditLog.cs               # Audit trail entity
│   │   └── NotificationSubscription.cs # Notification preferences entity
│   ├── Configurations/               # EF Core fluent API configurations
│   │   ├── OrderConfiguration.cs
│   │   ├── OrderStatusConfiguration.cs
│   │   ├── OrderFileConfiguration.cs
│   │   ├── OrderNoteConfiguration.cs
│   │   ├── Order3DPrintingAttributesConfiguration.cs
│   │   ├── OrderCncMachiningAttributesConfiguration.cs
│   │   ├── OrderSheetMetalAttributesConfiguration.cs
│   │   ├── Order3DScanningAttributesConfiguration.cs
│   │   ├── Order3DDesignAttributesConfiguration.cs
│   │   ├── ServiceCategoryConfiguration.cs
│   │   ├── ProcessTypeConfiguration.cs
│   │   ├── AuditLogConfiguration.cs
│   │   └── NotificationSubscriptionConfiguration.cs
│   ├── Migrations/                   # EF Core migrations (auto-generated)
│   └── Maliev.OrderService.Data.csproj
│
├── Maliev.OrderService.Tests/        # Test project
│   ├── Unit/
│   │   ├── Services/
│   │   │   ├── OrderServiceTests.cs
│   │   │   ├── FileServiceTests.cs
│   │   │   └── ValidationTests.cs
│   │   └── Controllers/
│   │       ├── OrdersControllerTests.cs
│   │       └── OrderStatusControllerTests.cs
│   ├── Integration/
│   │   ├── CustomerServiceIntegrationTests.cs
│   │   ├── PaymentServiceIntegrationTests.cs
│   │   ├── UploadServiceIntegrationTests.cs
│   │   └── DatabaseIntegrationTests.cs
│   ├── Contract/
│   │   ├── OrderEndpointContractTests.cs    # OpenAPI schema validation
│   │   ├── FileEndpointContractTests.cs
│   │   └── NotesEndpointContractTests.cs
│   └── Maliev.OrderService.Tests.csproj
│
├── .github/
│   └── workflows/
│       ├── ci-develop.yml            # CI for develop branch
│       ├── ci-staging.yml            # CI for staging branch
│       └── ci-main.yml               # CI for main branch
│
├── Maliev.OrderService.sln           # Solution file
├── Dockerfile                        # Container definition
├── .gitignore                        # Git ignore patterns
├── .editorconfig                     # Code style rules
└── CLAUDE.md                         # Agent context file (updated in Phase 1)
```

**Structure Decision**: Standard .NET microservice structure with three projects following MALIEV conventions:
1. **Maliev.OrderService.Api**: WebAPI layer (controllers, DTOs, middleware, service clients)
2. **Maliev.OrderService.Data**: Data layer (DbContext, entities, configurations, migrations)
3. **Maliev.OrderService.Tests**: Test layer (unit, integration, contract tests)

## Phase 0: Outline & Research

**Objective**: Resolve technical unknowns and document technology decisions before design phase.

### Research Tasks

1. **.NET 9.0 Best Practices**
   - **Task**: Research .NET 9.0 microservice patterns for stateless APIs
   - **Focus**: Minimal APIs vs Controllers, dependency injection patterns, configuration providers
   - **Output**: Decision on controller-based approach (aligns with MALIEV standards)

2. **Entity Framework Core 9.0 Patterns**
   - **Task**: Research EF Core 9.0 optimistic concurrency patterns
   - **Focus**: RowVersion vs manual timestamp, conflict resolution strategies
   - **Output**: Decision on RowVersion byte[] approach for concurrency control

3. **Multi-Service Integration Patterns**
   - **Task**: Research HttpClient best practices for 6 external service integrations
   - **Focus**: Typed clients, Polly retry policies, circuit breakers, timeout strategies
   - **Output**: Decision on IHttpClientFactory with named clients per service

4. **State Machine Implementation**
   - **Task**: Research patterns for 16-state order workflow with strict transitions
   - **Focus**: State pattern vs enum-based validation, transition rule storage
   - **Output**: Decision on enum-based states with validation lookup dictionary

5. **Batch Operations Strategy**
   - **Task**: Research all-or-nothing batch transaction patterns in EF Core
   - **Focus**: TransactionScope vs DbContext.Database.BeginTransaction, rollback strategies
   - **Output**: Decision on explicit transaction with try-catch rollback

6. **File Upload Integration**
   - **Task**: Research Upload Service integration patterns for retry logic
   - **Focus**: Polly exponential backoff, multipart form-data handling, object path patterns
   - **Output**: Decision on Polly policy with 3 retries, exponential backoff (2^attempt seconds)

7. **JWT Authentication Patterns**
   - **Task**: Research Auth Service integration for context-based authorization
   - **Focus**: Custom middleware vs built-in JWT bearer, user context extraction
   - **Output**: Decision on custom middleware that calls Auth Service `/validate` endpoint

8. **CORS Configuration Strategy**
   - **Task**: Research environment-specific CORS configuration patterns
   - **Focus**: ASPNETCORE_ENVIRONMENT-based origin selection, credential handling
   - **Output**: Decision on environment variable arrays for allowed origins

9. **Audit Logging Patterns**
   - **Task**: Research append-only audit table design for 7-year retention
   - **Focus**: Separate table vs temporal tables, query performance optimization
   - **Output**: Decision on separate AuditLog table with indexed timestamp and userId

10. **Data Retention Strategy**
    - **Task**: Research automatic file deletion patterns after 30 days
    - **Focus**: Background service vs scheduled job, soft delete vs hard delete
    - **Output**: Decision on Background Service with scoped service for periodic cleanup

**Output**: `research.md` with all decisions documented in format:
```markdown
## Decision: [Technology Choice]
**Rationale**: [Why chosen]
**Alternatives Considered**: [What else evaluated]
**Implementation Notes**: [Key patterns to follow]
```

## Phase 1: Design & Contracts

*Prerequisites: research.md complete*

### 1. Data Model Design (`data-model.md`)

**Entity Extraction from Spec**:

#### Core Entities

**Order**
- `OrderId` (string, PK) - Unique identifier (e.g., ORD-2025-00001)
- `CustomerId` (string) - Reference to Customer Service
- `CustomerType` (enum: Customer, Employee) - For RBAC context
- `ServiceCategoryId` (int, FK) - Reference to ServiceCategory
- `ProcessTypeId` (int, FK) - Reference to ProcessType
- `IsConfidential` (bool) - NDA-based confidentiality flag
- `PaymentId` (string, nullable) - Reference to Payment Service
- `PaymentStatus` (enum: Unpaid, Paid, POIssued) - Payment tracking
- `AssignedEmployeeId` (string, nullable) - Assigned employee reference
- `DepartmentId` (string, nullable) - Department assignment
- `Version` (byte[]) - RowVersion for optimistic concurrency
- `CreatedAt` (DateTime) - Creation timestamp
- `UpdatedAt` (DateTime) - Last update timestamp
- `CreatedBy` (string) - User who created order
- `UpdatedBy` (string) - User who last updated order

**OrderStatus**
- `StatusId` (int, PK) - Auto-increment ID
- `OrderId` (string, FK) - Reference to Order
- `Status` (enum: New, Reviewing, Rejected, Reviewed, Quoted, Declined, Accepted, Expired, Paid, POIssued, InProgress, OnHold, Finished, Shipped, Reopen, Cancelled) - 16 states
- `InternalNotes` (string, nullable, encrypted) - Employee-only notes
- `CustomerNotes` (string, nullable) - Customer-visible notes
- `Timestamp` (DateTime) - Status change time
- `UpdatedBy` (string) - Employee who updated status

**OrderFile**
- `FileId` (int, PK) - Auto-increment ID
- `OrderId` (string, FK) - Reference to Order
- `ObjectPath` (string) - GCS object path (e.g., "orders/ORD-001/files/drawing.pdf")
- `FileName` (string) - Original filename
- `FileSize` (long) - Size in bytes (max 100MB)
- `FileType` (string) - MIME type or extension
- `AccessLevel` (enum: Internal, Confidential) - Access control
- `UploadedAt` (DateTime) - Upload timestamp
- `UploadedBy` (string) - User who uploaded
- `DeletedAt` (DateTime, nullable) - Soft delete timestamp (30-day retention)

**ServiceCategory**
- `CategoryId` (int, PK) - Auto-increment ID
- `Name` (string) - Category name (e.g., "3D Printing")
- `Description` (string) - Category description
- `IsActive` (bool) - Active/inactive flag

**ProcessType**
- `ProcessTypeId` (int, PK) - Auto-increment ID
- `ServiceCategoryId` (int, FK) - Reference to ServiceCategory
- `Name` (string) - Process name (e.g., "FDM", "SLA")
- `Description` (string) - Process description
- `IsActive` (bool) - Active/inactive flag

**AuditLog**
- `AuditId` (long, PK) - Auto-increment ID
- `OrderId` (string, FK) - Reference to Order
- `Action` (enum: Create, Update, Cancel, StatusChange, FileUpload, FileDelete) - Action type
- `PerformedBy` (string) - User who performed action
- `PerformedAt` (DateTime) - Action timestamp
- `EntityType` (string) - Entity affected (Order, OrderStatus, OrderFile)
- `EntityId` (string) - ID of affected entity
- `ChangeDetails` (string, JSON) - Serialized before/after state

**NotificationSubscription**
- `SubscriptionId` (int, PK) - Auto-increment ID
- `CustomerId` (string) - Reference to Customer Service
- `IsSubscribed` (bool) - Opt-in status
- `Channels` (string, JSON array) - ["LINE", "Email"] channels

#### Relationships

- `Order` 1:N `OrderStatus` (one order has many status history entries)
- `Order` 1:N `OrderFile` (one order has many files)
- `Order` N:1 `ServiceCategory` (many orders belong to one category)
- `Order` N:1 `ProcessType` (many orders use one process type)
- `ServiceCategory` 1:N `ProcessType` (one category has many process types)
- `Order` 1:N `AuditLog` (one order has many audit entries)

#### State Transitions (Validation Rules)

```csharp
// Valid state transitions dictionary
Dictionary<OrderStatusEnum, List<OrderStatusEnum>> ValidTransitions = new()
{
    [New] = [Reviewing, Cancelled],
    [Reviewing] = [Rejected, Reviewed, Cancelled],
    [Rejected] = [], // Terminal
    [Reviewed] = [Quoted, Cancelled],
    [Quoted] = [Declined, Accepted, Expired, Cancelled],
    [Declined] = [], // Terminal
    [Expired] = [], // Terminal
    [Accepted] = [Paid, POIssued, Cancelled],
    [Paid] = [InProgress, Cancelled],
    [POIssued] = [InProgress, Cancelled],
    [InProgress] = [OnHold, Finished, Cancelled],
    [OnHold] = [InProgress, Cancelled],
    [Finished] = [Shipped, Reopen],
    [Shipped] = [Reopen], // Can reopen after shipped
    [Reopen] = [InProgress],
    [Cancelled] = [] // Terminal
};
```

### 2. API Contracts (`contracts/`)

**Contract Generation from Functional Requirements**:

#### Orders Controller (`contracts/orders.yaml`)

```yaml
# GET /api/v1/orders
- Summary: List orders with filtering and pagination
- Auth: JWT required
- Query Params: customerId, status, serviceCategory, page, pageSize
- Response: PaginatedList<OrderDto>
- RBAC: Customer sees own orders, Employee sees assigned, Manager sees department, Admin sees all

# GET /api/v1/orders/{orderId}
- Summary: Get order details
- Auth: JWT required
- Path Params: orderId
- Response: OrderDto
- RBAC: Based on user context

# POST /api/v1/orders
- Summary: Create new order
- Auth: JWT required
- Request Body: CreateOrderRequest
- Response: OrderDto (201 Created)
- Validation: Customer Service API must be available, NDA status must be retrieved

# POST /api/v1/orders/batch
- Summary: Batch create orders
- Auth: JWT required (Employee+ only)
- Request Body: List<CreateOrderRequest>
- Response: List<OrderDto> (201 Created)
- Transaction: All-or-nothing with rollback on any failure

# PUT /api/v1/orders/{orderId}
- Summary: Update order
- Auth: JWT required
- Path Params: orderId
- Request Body: UpdateOrderRequest (includes version for optimistic concurrency)
- Response: OrderDto
- Concurrency: 409 Conflict if version mismatch

# PUT /api/v1/orders/batch
- Summary: Batch update orders
- Auth: JWT required (Employee+ only)
- Request Body: List<UpdateOrderRequest>
- Response: List<OrderDto>
- Transaction: All-or-nothing with rollback

# DELETE /api/v1/orders/{orderId}
- Summary: Cancel/delete order
- Auth: JWT required
- Path Params: orderId
- Query Params: reason, calculatePartialCharge (bool)
- Response: CancellationResult (with refund amount if applicable)
- Integration: Calls Payment Service for refund/partial charge

# DELETE /api/v1/orders/batch
- Summary: Batch cancel orders
- Auth: JWT required (Employee+ only)
- Request Body: List<CancelOrderRequest>
- Response: List<CancellationResult>
- Transaction: All-or-nothing with rollback
```

#### Order Status Controller (`contracts/statuses.yaml`)

```yaml
# GET /api/v1/orders/{orderId}/statuses
- Summary: Get order status history
- Auth: JWT required
- Path Params: orderId
- Response: List<OrderStatusDto>
- Note: Internal notes filtered for Customer role

# POST /api/v1/orders/{orderId}/statuses
- Summary: Update order status
- Auth: JWT required (Employee+ only)
- Path Params: orderId
- Request Body: UpdateStatusRequest (status, internalNotes, customerNotes)
- Response: OrderStatusDto
- Validation: State transition must be valid per ValidTransitions dictionary
- Notification: Triggers notification via Notification Service
```

#### Order Files Controller (`contracts/files.yaml`)

```yaml
# GET /api/v1/orders/{orderId}/files
- Summary: List order files
- Auth: JWT required
- Path Params: orderId
- Response: List<OrderFileDto>

# POST /api/v1/orders/{orderId}/files
- Summary: Upload file to order
- Auth: JWT required
- Path Params: orderId
- Request Body: multipart/form-data (file, accessLevel)
- Response: OrderFileDto (201 Created)
- Validation: Max 100MB per file, 500MB total per order, supported formats only
- Integration: Calls Upload Service with retry policy (3 attempts, exponential backoff)
- ObjectPath Pattern: "orders/{orderId}/files/{filename}"

# GET /api/v1/orders/{orderId}/files/{fileId}/download
- Summary: Download file
- Auth: JWT required
- Path Params: orderId, fileId
- Response: File stream (Content-Disposition: attachment)
- Integration: Calls Upload Service GET /v1/files/path?objectPath={path}

# DELETE /api/v1/orders/{orderId}/files/{fileId}
- Summary: Delete file
- Auth: JWT required
- Path Params: orderId, fileId
- Response: 204 No Content
- Integration: Calls Upload Service DELETE /v1/files/path?objectPath={path}
- Note: Soft delete in database, actual GCS deletion after 30 days
```

### 3. Contract Tests (Failing Tests)

**Test Files** (in `Maliev.OrderService.Tests/Contract/`):

```csharp
// OrderEndpointContractTests.cs
[Fact]
public async Task GET_Orders_Returns_PaginatedList_Schema()
{
    // Arrange: Load OpenAPI spec
    var spec = LoadOpenApiSpec("contracts/orders.yaml");

    // Act: Make request (will fail - no implementation yet)
    var response = await _client.GetAsync("/api/v1/orders");

    // Assert: Validate response against schema
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    ValidateJsonSchema(response.Content, spec.GetSchema("PaginatedListOrderDto"));
}

[Fact]
public async Task POST_Orders_With_Invalid_Customer_Returns_400()
{
    // This test MUST fail until implementation is complete
    var request = new CreateOrderRequest { CustomerId = "INVALID" };
    var response = await _client.PostAsJsonAsync("/api/v1/orders", request);
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
}

// ... 20+ contract tests covering all endpoints
```

### 4. Quickstart Test Scenarios (`quickstart.md`)

**Scenario 1: Customer Creates Confidential Order**
```bash
# Prerequisites: Customer with NDA signed in Customer Service
export CUSTOMER_ID="CUST-001"
export JWT_TOKEN="<customer-jwt-token>"

# Step 1: Create order (auto-confidential due to NDA)
curl -X POST https://api.maliev.com/orders/v1/orders \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "CUST-001",
    "serviceCategoryId": 1,
    "processTypeId": 3,
    "requirements": "3D print prototype with FDM"
  }'

# Expected: 201 Created with isConfidential=true
# Validation: Customer Service /nda endpoint called, order marked confidential
```

**Scenario 2: Employee Updates Order Status with Notes**
```bash
# Prerequisites: Employee JWT token
export ORDER_ID="ORD-2025-00001"
export JWT_TOKEN="<employee-jwt-token>"

# Step 1: Update status from New → Reviewing
curl -X POST https://api.maliev.com/orders/v1/orders/$ORDER_ID/statuses \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "status": "Reviewing",
    "internalNotes": "Waiting for material supplier response",
    "customerNotes": "Your order is being reviewed by our team"
  }'

# Expected: 200 OK with new status entry
# Validation: Notification Service called, customer receives email/LINE notification
```

**Scenario 3: Batch Operation with Rollback**
```bash
# Step 1: Attempt batch update with one invalid order
curl -X PUT https://api.maliev.com/orders/v1/orders/batch \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '[
    {"orderId": "ORD-001", "version": "AAAAAA==", "assignedEmployeeId": "EMP-001"},
    {"orderId": "INVALID", "version": "AAAAAA==", "assignedEmployeeId": "EMP-002"}
  ]'

# Expected: 400 Bad Request with error details
# Validation: Database transaction rolled back, ORD-001 NOT updated
```

**Scenario 4: File Upload with Retry**
```bash
# Step 1: Upload large CAD file (triggers retry on network failure)
curl -X POST https://api.maliev.com/orders/v1/orders/$ORDER_ID/files \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -F "file=@design.stl" \
  -F "accessLevel=Confidential"

# Expected: 201 Created with file metadata
# Validation: Upload Service called with retry (3 attempts, exponential backoff)
# ObjectPath: "orders/ORD-2025-00001/files/design.stl"
```

**Scenario 5: Order Cancellation with Partial Charge**
```bash
# Step 1: Cancel order in InProgress status
curl -X DELETE https://api.maliev.com/orders/v1/orders/$ORDER_ID?calculatePartialCharge=true \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"reason": "Customer changed requirements"}'

# Expected: 200 OK with refund calculation
# Validation: Payment Service /refunds called, partial charge for work completed
```

### 5. Agent Context File Update (`CLAUDE.md`)

**Update Strategy**:
- Run `.specify/scripts/powershell/update-agent-context.ps1 -AgentType claude`
- Add Order Service-specific context between auto-generated markers
- Document 16-state workflow, external service endpoints, RBAC patterns
- Keep total file under 150 lines for token efficiency

**New Content to Add**:
```markdown
## Order Service Patterns

### State Machine Validation
- 16 states: New → Reviewing → [Rejected|Reviewed] → Quoted → [Declined|Accepted|Expired] → [Paid|POIssued] → InProgress → Finished → Shipped
- Exception flows: InProgress ↔ OnHold, Finished/Shipped → Reopen, Any → Cancelled
- ValidTransitions dictionary enforces rules

### External Service Integration
- Customer Service: NDA validation, customer details
- Payment Service: Payment status, refunds, partial charges
- Upload Service: File operations with retry (3 attempts, exponential backoff)
- Auth Service: JWT validation, user context (userType, userId, roles)
- Employee Service: Employee details, department listing
- Notification Service: Multi-channel notifications (LINE, email)

### RBAC Context-Based Authorization
- Auth Service returns: { userType: "customer"|"employee", userId, roles[] }
- Customer: Own orders only
- Employee: Assigned orders
- Manager: Department orders
- Admin: All orders
```

**Output**: Updated `CLAUDE.md` with Order Service context, recent changes, and implementation patterns

## Phase 2: Task Planning Approach
*This section describes what the /tasks command will do - DO NOT execute during /plan*

**Task Generation Strategy**:

1. **Load Base Template**
   - Load `.specify/templates/tasks-template.md` as foundation
   - Preserve constitution validation tasks, security audit tasks

2. **Generate from Contracts** (Phase 1 output)
   - For each endpoint in `contracts/*.yaml`:
     - Create contract test task: "Write failing contract test for [endpoint]" [P]
     - Create implementation task: "Implement [endpoint] to pass contract test"
   - Example:
     - Task 5 [P]: Write failing contract test for GET /api/v1/orders
     - Task 15: Implement GET /api/v1/orders to pass contract test

3. **Generate from Data Model** (Phase 1 output)
   - For each entity in `data-model.md`:
     - Create entity task: "Create [Entity] model and EF Core configuration" [P]
     - Create migration task: "Generate and apply EF Core migration for [Entity]"
   - Example:
     - Task 1 [P]: Create Order entity and EF Core configuration
     - Task 2 [P]: Create OrderStatus entity and EF Core configuration

4. **Generate from Quickstart Scenarios** (Phase 1 output)
   - For each scenario in `quickstart.md`:
     - Create integration test task: "Write integration test for [scenario]"
   - Example:
     - Task 25: Write integration test for customer creates confidential order
     - Task 26: Write integration test for employee updates status with notes

5. **Generate Service Client Tasks**
   - For each external service (6 services):
     - Create client interface task: "Create I[Service]Client interface" [P]
     - Create client implementation task: "Implement [Service]Client with retry policy"
     - Create integration test task: "Write integration test for [Service]Client"
   - Example:
     - Task 10 [P]: Create ICustomerServiceClient interface
     - Task 20: Implement CustomerServiceClient with Polly retry
     - Task 30: Write integration test for CustomerServiceClient

6. **Generate Middleware Tasks**
   - Task: "Implement JwtAuthenticationMiddleware for Auth Service integration"
   - Task: "Implement ExceptionHandlingMiddleware for global error handling"
   - Task: "Configure CORS with environment-specific origins"

7. **Generate Validation Tasks** (from constitution)
   - Task: "Run security audit - verify no secrets in source code"
   - Task: "Run artifact cleanup - remove unused boilerplate files"
   - Task: "Validate zero warnings in build"
   - Task: "Validate 80% test coverage for business-critical logic"

**Ordering Strategy**:
1. **Foundation (Tasks 1-10)**: Data models, DbContext, migrations [P = parallel]
2. **Infrastructure (Tasks 11-20)**: Service clients, middleware, validators [P]
3. **Contract Tests (Tasks 21-35)**: All endpoint contract tests [P]
4. **Implementation (Tasks 36-60)**: Controllers, services to pass tests
5. **Integration Tests (Tasks 61-75)**: Quickstart scenarios, service integrations
6. **Validation (Tasks 76-80)**: Security audit, coverage check, zero warnings

**Estimated Output**: 80 numbered, ordered tasks in tasks.md with:
- [P] markers for parallel execution (independent files/tests)
- Clear dependencies (e.g., "After Task 5 passes")
- Constitution compliance tasks at end
- TDD order enforced (tests before implementation)

**IMPORTANT**: This phase is executed by the /tasks command, NOT by /plan

## Phase 3+: Future Implementation
*These phases are beyond the scope of the /plan command*

**Phase 3**: Task execution (/tasks command creates tasks.md)
**Phase 4**: Implementation (execute tasks.md following constitutional principles)
- Implement all 80 tasks in dependency order
- Follow TDD cycle: Red (failing test) → Green (minimal implementation) → Refactor
- Use AutoMapper for DTO mapping, FluentValidation for request validation
- Configure Polly policies for all HTTP clients (3 retries, exponential backoff)
- Implement state machine validation with ValidTransitions dictionary
- Configure environment-based service endpoints via IConfiguration
- Add Serilog JSON logging to all controllers and services

**Phase 5**: Validation (run tests, execute quickstart.md, performance validation)
- All contract tests pass (100% OpenAPI compliance)
- All integration tests pass (quickstart scenarios validate end-to-end)
- 80%+ coverage on business-critical logic (OrderService, state machine, RBAC)
- Zero warnings in build (`dotnet build`)
- Security audit passes (no secrets in code, environment variables only)
- Performance validation: <200ms p95, 500+ req/s under load

## Complexity Tracking
*Fill ONLY if Constitution Check has violations that must be justified*

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| N/A | All constitutional principles satisfied | No violations identified |

## Progress Tracking
*This checklist is updated during execution flow*

**Phase Status**:
- [x] Phase 0: Research complete (/plan command) ✅
- [x] Phase 1: Design complete (/plan command) ✅
- [x] Phase 2: Task planning approach documented (/plan command) ✅
- [ ] Phase 3: Tasks generated (/tasks command)
- [ ] Phase 4: Implementation complete
- [ ] Phase 5: Validation passed

**Gate Status**:
- [x] Initial Constitution Check: PASS (all 9 principles satisfied) ✅
- [x] Post-Design Constitution Check: PASS ✅ (validated - no violations introduced)
- [x] All NEEDS CLARIFICATION resolved ✅ (Phase 0 research complete)
- [x] Complexity deviations documented (none required) ✅

---
*Based on Constitution v1.0.0 - See `.specify/memory/constitution.md`*
