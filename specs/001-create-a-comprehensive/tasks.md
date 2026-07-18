# Tasks: Order Service API for Rapid Prototyping & Manufacturing

**Input**: Design documents from `/specs/001-create-a-comprehensive/`
**Prerequisites**: plan.md (✓), research.md (✓), data-model.md (✓), contracts/openapi.yaml (✓), quickstart.md (✓)

## Execution Flow
```
1. Load plan.md → Tech stack: .NET 9, PostgreSQL 18, EF Core 9.0.9
2. Load data-model.md → 13 entities identified
3. Load contracts/openapi.yaml → 4 endpoint groups (Orders, Status, Files, Notes)
4. Load quickstart.md → 8 integration test scenarios
5. Generate 90+ tasks across 5 phases
6. Apply TDD: Tests before implementation
7. Mark [P] for parallel execution (different files)
8. Validate: All contracts tested, all entities modeled
```

## Format: `[ID] [P?] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- Paths: `Maliev.OrderService.Api/`, `Maliev.OrderService.Data/`, `Maliev.OrderService.Tests/`

## Phase 3.1: Project Setup & Infrastructure
- [X] **T001** Create solution structure with 3 projects (Api, Data, Tests)
- [X] **T002** Initialize Maliev.OrderService.Api (.NET 9 WebAPI) with dependencies (ASP.NET Core 9.0, Serilog 8.0.2, AutoMapper, FluentValidation, Asp.Versioning)
- [X] **T003** Initialize Maliev.OrderService.Data (.NET 9 Class Library) with dependencies (EF Core 9.0.9, Npgsql 9.0.2)
- [X] **T004** Initialize Maliev.OrderService.Tests (.NET 9 Test Project) with dependencies (xUnit, Moq 4.20.72, FluentAssertions 8.6.0)
- [X] **T005** Configure .editorconfig with strict code analysis rules and TreatWarningsAsErrors
- [X] **T006** Configure .gitignore to exclude bin/, obj/, .vs/, *.user, .vscode/
- [X] **T007** Remove default WeatherForecast controller and model from Maliev.OrderService.Api
- [X] **T008** Configure Serilog for console-only JSON logging in Program.cs
- [X] **T009** Configure Google Secret Manager integration with /mnt/secrets pattern in Program.cs
- [X] **T010** Configure CORS with CORS_ALLOWED_ORIGINS environment variable in Program.cs
- [X] **T011** Configure health checks (/orders/liveness and /orders/readiness) in Program.cs
- [X] **T012** Configure Swagger UI at /orders/swagger route in Program.cs
- [X] **T013** Configure middleware pipeline order (Swagger → HTTPS → RateLimiter → Auth → Authorization) in Program.cs

## Phase 3.2: Database Models & Configuration (TDD - Models First)
**Entity Models (can run in parallel)**
- [X] **T014** [P] Create Order entity model in Maliev.OrderService.Data/Models/Order.cs (30 properties: materials, quantity, dates, audit)
- [X] **T015** [P] Create OrderStatus entity model in Maliev.OrderService.Data/Models/OrderStatus.cs (status history with notes)
- [X] **T016** [P] Create OrderFile entity model in Maliev.OrderService.Data/Models/OrderFile.cs (with file_role, file_category, design_units)
- [X] **T017** [P] Create OrderNote entity model in Maliev.OrderService.Data/Models/OrderNote.cs (customer/internal types)
- [X] **T018** [P] Create Order3DPrintingAttributes entity model in Maliev.OrderService.Data/Models/Order3DPrintingAttributes.cs
- [X] **T019** [P] Create OrderCncMachiningAttributes entity model in Maliev.OrderService.Data/Models/OrderCncMachiningAttributes.cs
- [X] **T020** [P] Create OrderSheetMetalAttributes entity model in Maliev.OrderService.Data/Models/OrderSheetMetalAttributes.cs
- [X] **T021** [P] Create Order3DScanningAttributes entity model in Maliev.OrderService.Data/Models/Order3DScanningAttributes.cs
- [X] **T022** [P] Create Order3DDesignAttributes entity model in Maliev.OrderService.Data/Models/Order3DDesignAttributes.cs
- [X] **T023** [P] Create ServiceCategory entity model in Maliev.OrderService.Data/Models/ServiceCategory.cs
- [X] **T024** [P] Create ProcessType entity model in Maliev.OrderService.Data/Models/ProcessType.cs
- [X] **T025** [P] Create AuditLog entity model in Maliev.OrderService.Data/Models/AuditLog.cs (append-only, 7-year retention)
- [X] **T026** [P] Create NotificationSubscription entity model in Maliev.OrderService.Data/Models/NotificationSubscription.cs

**EF Core Configurations (can run in parallel)**
- [X] **T027** [P] Create OrderConfiguration with FluentAPI in Maliev.OrderService.Data/Configurations/OrderConfiguration.cs (indexes, computed columns)
- [X] **T028** [P] Create OrderStatusConfiguration with FluentAPI in Maliev.OrderService.Data/Configurations/OrderStatusConfiguration.cs (encrypted InternalNotes)
- [X] **T029** [P] Create OrderFileConfiguration with FluentAPI in Maliev.OrderService.Data/Configurations/OrderFileConfiguration.cs
- [X] **T030** [P] Create OrderNoteConfiguration with FluentAPI in Maliev.OrderService.Data/Configurations/OrderNoteConfiguration.cs
- [X] **T031** [P] Create Order3DPrintingAttributesConfiguration in Maliev.OrderService.Data/Configurations/Order3DPrintingAttributesConfiguration.cs
- [X] **T032** [P] Create OrderCncMachiningAttributesConfiguration in Maliev.OrderService.Data/Configurations/OrderCncMachiningAttributesConfiguration.cs
- [X] **T033** [P] Create OrderSheetMetalAttributesConfiguration in Maliev.OrderService.Data/Configurations/OrderSheetMetalAttributesConfiguration.cs
- [X] **T034** [P] Create Order3DScanningAttributesConfiguration in Maliev.OrderService.Data/Configurations/Order3DScanningAttributesConfiguration.cs
- [X] **T035** [P] Create Order3DDesignAttributesConfiguration in Maliev.OrderService.Data/Configurations/Order3DDesignAttributesConfiguration.cs
- [X] **T036** [P] Create ServiceCategoryConfiguration with FluentAPI in Maliev.OrderService.Data/Configurations/ServiceCategoryConfiguration.cs
- [X] **T037** [P] Create ProcessTypeConfiguration with FluentAPI in Maliev.OrderService.Data/Configurations/ProcessTypeConfiguration.cs
- [X] **T038** [P] Create AuditLogConfiguration with FluentAPI in Maliev.OrderService.Data/Configurations/AuditLogConfiguration.cs
- [X] **T039** [P] Create NotificationSubscriptionConfiguration in Maliev.OrderService.Data/Configurations/NotificationSubscriptionConfiguration.cs

**DbContext & Migrations**
- [X] **T040** Create OrderDbContext in Maliev.OrderService.Data/OrderDbContext.cs (13 DbSets, apply configurations)
- [X] **T041** Create initial EF Core migration (InitialCreate) with dotnet ef migrations add
- [X] **T042** Create seed data migration for ServiceCategory and ProcessType (11 categories, 20+ process types)
- [X] **T043** Test migration with local PostgreSQL 18 database (dotnet ef database update)

## Phase 3.3: Contract Tests (TDD - Tests Before Implementation)
**CRITICAL: All tests MUST be written and MUST FAIL before implementation**

**Order Endpoint Tests (can run in parallel)**
- [ ] **T044** [P] Contract test GET /orders in Maliev.OrderService.Tests/Contract/OrderEndpointTests.cs (pagination, filtering)
- [ ] **T045** [P] Contract test POST /orders in Maliev.OrderService.Tests/Contract/OrderEndpointTests.cs (create with validation)
- [ ] **T046** [P] Contract test GET /orders/{orderId} in Maliev.OrderService.Tests/Contract/OrderEndpointTests.cs
- [ ] **T047** [P] Contract test PUT /orders/{orderId} in Maliev.OrderService.Tests/Contract/OrderEndpointTests.cs (optimistic concurrency)
- [ ] **T048** [P] Contract test DELETE /orders/{orderId} in Maliev.OrderService.Tests/Contract/OrderEndpointTests.cs (cancellation)
- [ ] **T049** [P] Contract test POST /orders/batch in Maliev.OrderService.Tests/Contract/BatchOrderEndpointTests.cs (batch create)
- [ ] **T050** [P] Contract test PUT /orders/batch in Maliev.OrderService.Tests/Contract/BatchOrderEndpointTests.cs (batch update)
- [ ] **T051** [P] Contract test DELETE /orders/batch in Maliev.OrderService.Tests/Contract/BatchOrderEndpointTests.cs (batch cancel)

**Status Endpoint Tests (can run in parallel)**
- [ ] **T052** [P] Contract test GET /orders/{orderId}/statuses in Maliev.OrderService.Tests/Contract/StatusEndpointTests.cs (history with filtering)
- [ ] **T053** [P] Contract test POST /orders/{orderId}/statuses in Maliev.OrderService.Tests/Contract/StatusEndpointTests.cs (state transitions)

**File Endpoint Tests (can run in parallel)**
- [ ] **T054** [P] Contract test GET /orders/{orderId}/files in Maliev.OrderService.Tests/Contract/FileEndpointTests.cs
- [ ] **T055** [P] Contract test POST /orders/{orderId}/files in Maliev.OrderService.Tests/Contract/FileEndpointTests.cs (multipart upload)
- [ ] **T056** [P] Contract test GET /orders/{orderId}/files/{fileId} in Maliev.OrderService.Tests/Contract/FileEndpointTests.cs (download)
- [ ] **T057** [P] Contract test DELETE /orders/{orderId}/files/{fileId} in Maliev.OrderService.Tests/Contract/FileEndpointTests.cs

**Notes Endpoint Tests (can run in parallel)**
- [ ] **T058** [P] Contract test GET /orders/{orderId}/notes in Maliev.OrderService.Tests/Contract/NotesEndpointTests.cs
- [ ] **T059** [P] Contract test POST /orders/{orderId}/notes in Maliev.OrderService.Tests/Contract/NotesEndpointTests.cs (RBAC enforcement)

**Integration Tests (can run in parallel)**
- [ ] **T060** [P] Integration test Scenario 1: Customer creates confidential order (auto NDA) in Maliev.OrderService.Tests/Integration/ConfidentialOrderTests.cs
- [ ] **T061** [P] Integration test Scenario 2: Employee updates status with dual notes in Maliev.OrderService.Tests/Integration/StatusUpdateTests.cs
- [ ] **T062** [P] Integration test Scenario 3: Batch operation with all-or-nothing rollback in Maliev.OrderService.Tests/Integration/BatchOperationTests.cs
- [ ] **T063** [P] Integration test Scenario 4: File upload with retry and size validation in Maliev.OrderService.Tests/Integration/FileUploadTests.cs
- [ ] **T064** [P] Integration test Scenario 5: Order cancellation with partial charge in Maliev.OrderService.Tests/Integration/CancellationTests.cs
- [ ] **T065** [P] Integration test Scenario 6: Optimistic concurrency conflict in Maliev.OrderService.Tests/Integration/ConcurrencyTests.cs
- [ ] **T066** [P] Integration test Scenario 7: RBAC context-based authorization in Maliev.OrderService.Tests/Integration/RbacAuthorizationTests.cs
- [ ] **T067** [P] Integration test: Material Service validation (invalid materialId, colorId, surfaceFinishingId) in Maliev.OrderService.Tests/Integration/MaterialServiceTests.cs

## Phase 3.4: DTOs & Mapping (ONLY after tests are failing)
**DTOs (can run in parallel)**
- [ ] **T068** [P] Create OrderDto in Maliev.OrderService.Api/Models/DTOs/OrderDto.cs (with all 30 properties from OpenAPI)
- [ ] **T069** [P] Create CreateOrderRequest in Maliev.OrderService.Api/Models/DTOs/CreateOrderRequest.cs
- [ ] **T070** [P] Create UpdateOrderRequest in Maliev.OrderService.Api/Models/DTOs/UpdateOrderRequest.cs (with version for optimistic locking)
- [ ] **T071** [P] Create OrderStatusDto in Maliev.OrderService.Api/Models/DTOs/OrderStatusDto.cs
- [ ] **T072** [P] Create UpdateStatusRequest in Maliev.OrderService.Api/Models/DTOs/UpdateStatusRequest.cs
- [ ] **T073** [P] Create OrderFileDto in Maliev.OrderService.Api/Models/DTOs/OrderFileDto.cs (with file_role, file_category, design_units)
- [ ] **T074** [P] Create OrderNoteDto in Maliev.OrderService.Api/Models/DTOs/OrderNoteDto.cs
- [ ] **T075** [P] Create CreateOrderNoteRequest in Maliev.OrderService.Api/Models/DTOs/CreateOrderNoteRequest.cs
- [ ] **T076** [P] Create CancelOrderRequest in Maliev.OrderService.Api/Models/DTOs/CancelOrderRequest.cs
- [ ] **T077** [P] Create CancellationResult in Maliev.OrderService.Api/Models/DTOs/CancellationResult.cs
- [ ] **T078** [P] Create PaginatedOrderListResponse in Maliev.OrderService.Api/Models/DTOs/PaginatedOrderListResponse.cs
- [ ] **T079** [P] Create ErrorResponse in Maliev.OrderService.Api/Models/DTOs/ErrorResponse.cs

**AutoMapper Profile**
- [ ] **T080** Create MappingProfile with all entity-to-DTO mappings in Maliev.OrderService.Api/Profiles/MappingProfile.cs

## Phase 3.5: Validators (FluentValidation)
**Validators (can run in parallel)**
- [ ] **T081** [P] Create CreateOrderRequestValidator in Maliev.OrderService.Api/Validators/CreateOrderRequestValidator.cs (ServiceCategory required, ProcessType optional)
- [ ] **T082** [P] Create UpdateOrderRequestValidator in Maliev.OrderService.Api/Validators/UpdateOrderRequestValidator.cs (version required, quantity validation)
- [ ] **T083** [P] Create UpdateStatusRequestValidator in Maliev.OrderService.Api/Validators/UpdateStatusRequestValidator.cs (valid state transitions)
- [ ] **T084** [P] Create CreateOrderNoteRequestValidator in Maliev.OrderService.Api/Validators/CreateOrderNoteRequestValidator.cs (noteText max 10000 chars)

## Phase 3.6: External Service Clients
**Service Client Interfaces (can run in parallel)**
- [ ] **T085** [P] Create ICustomerServiceClient interface in Maliev.OrderService.Api/Services/ICustomerServiceClient.cs (GetNdaStatusAsync)
- [ ] **T086** [P] Create IMaterialServiceClient interface in Maliev.OrderService.Api/Services/IMaterialServiceClient.cs (ValidateMaterialAsync, GetMaterialAsync)
- [ ] **T087** [P] Create IPaymentServiceClient interface in Maliev.OrderService.Api/Services/IPaymentServiceClient.cs (CalculatePartialChargeAsync)
- [ ] **T088** [P] Create IFileServiceClient interface in Maliev.OrderService.Api/Services/IFileServiceClient.cs (UploadFileAsync with retry)
- [ ] **T089** [P] Create IAuthServiceClient interface in Maliev.OrderService.Api/Services/IAuthServiceClient.cs (ValidateJwtAsync)
- [ ] **T090** [P] Create IEmployeeServiceClient interface in Maliev.OrderService.Api/Services/IEmployeeServiceClient.cs (GetEmployeeAsync)
- [ ] **T091** [P] Create INotificationServiceClient interface in Maliev.OrderService.Api/Services/INotificationServiceClient.cs (SendOrderUpdateAsync)

**Service Client Implementations (can run in parallel)**
- [ ] **T092** [P] Implement CustomerServiceClient in Maliev.OrderService.Api/Services/CustomerServiceClient.cs (HttpClient with CUSTOMER_SERVICE_URL)
- [ ] **T093** [P] Implement MaterialServiceClient in Maliev.OrderService.Api/Services/MaterialServiceClient.cs (HttpClient with MATERIAL_SERVICE_URL, 24hr cache refresh)
- [ ] **T094** [P] Implement PaymentServiceClient in Maliev.OrderService.Api/Services/PaymentServiceClient.cs (HttpClient with PAYMENT_SERVICE_URL)
- [ ] **T095** [P] Implement FileServiceClient in Maliev.OrderService.Api/Services/FileServiceClient.cs (HttpClient with UPLOAD_SERVICE_URL, Polly retry 3x exponential backoff)
- [ ] **T096** [P] Implement AuthServiceClient in Maliev.OrderService.Api/Services/AuthServiceClient.cs (HttpClient with AUTH_SERVICE_URL)
- [ ] **T097** [P] Implement EmployeeServiceClient in Maliev.OrderService.Api/Services/EmployeeServiceClient.cs (HttpClient with EMPLOYEE_SERVICE_URL)
- [ ] **T098** [P] Implement NotificationServiceClient in Maliev.OrderService.Api/Services/NotificationServiceClient.cs (HttpClient with NOTIFICATION_SERVICE_URL)

**Register HttpClients in Program.cs**
- [ ] **T099** Register 7 named HttpClients with environment-based BaseAddress and Polly retry policies in Program.cs

## Phase 3.7: Business Logic Services
**Core Services**
- [ ] **T100** Create IOrderService interface in Maliev.OrderService.Api/Services/IOrderService.cs (CRUD, batch, state machine, RBAC)
- [ ] **T101** Implement OrderService in Maliev.OrderService.Api/Services/OrderService.cs (16-state workflow with ValidTransitions dictionary)
- [ ] **T102** Add ValidTransitions dictionary to OrderService (16 states, 30+ transitions)
- [ ] **T103** Add RBAC authorization logic to OrderService (Customer/Employee/Manager/Admin context-based filtering)
- [ ] **T104** Add optimistic concurrency handling to OrderService (RowVersion conflict detection)
- [ ] **T105** Add batch operation transaction logic to OrderService (all-or-nothing rollback)
- [ ] **T106** Add Material Service integration to OrderService (validate materialId/colorId/surfaceFinishingId, cache refresh)
- [ ] **T107** Add Customer Service integration to OrderService (auto NDA detection for IsConfidential)
- [ ] **T108** Add AuditLog creation logic to OrderService (append-only for all sensitive operations)
- [ ] **T109** Add Notification Service integration to OrderService (trigger on status change)

**File Service**
- [ ] **T110** Create IFileService interface in Maliev.OrderService.Api/Services/IFileService.cs
- [ ] **T111** Implement FileService in Maliev.OrderService.Api/Services/FileService.cs (Upload Service integration, 100MB/file, 500MB/order limits)
- [ ] **T112** Add file role validation to FileService (designUnits required for CAD files, null for others)

**Notes Service**
- [ ] **T113** Create IOrderNoteService interface in Maliev.OrderService.Api/Services/IOrderNoteService.cs
- [ ] **T114** Implement OrderNoteService in Maliev.OrderService.Api/Services/OrderNoteService.cs (RBAC filtering for internal notes)

**Background Services**
- [ ] **T115** Create FileCleanupService (IHostedService) in Maliev.OrderService.Api/Services/FileCleanupService.cs (30-day soft delete after terminal state)
- [ ] **T116** Create MaterialCacheRefreshService (IHostedService) in Maliev.OrderService.Api/Services/MaterialCacheRefreshService.cs (24-hour TTL refresh)

## Phase 3.8: API Controllers
**Controllers (sequential - modify same shared services)**
- [ ] **T117** Create OrdersController in Maliev.OrderService.Api/Controllers/OrdersController.cs (GET /orders with pagination/filtering)
- [ ] **T118** Add POST /orders endpoint to OrdersController (create with Customer Service NDA check)
- [ ] **T119** Add GET /orders/{orderId} endpoint to OrdersController (RBAC filtering)
- [ ] **T120** Add PUT /orders/{orderId} endpoint to OrdersController (optimistic concurrency check)
- [ ] **T121** Add DELETE /orders/{orderId} endpoint to OrdersController (cancellation with partial charge)
- [ ] **T122** Add POST /orders/batch endpoint to OrdersController (batch create with transaction)
- [ ] **T123** Add PUT /orders/batch endpoint to OrdersController (batch update with transaction)
- [ ] **T124** Add DELETE /orders/batch endpoint to OrdersController (batch cancel with transaction)

**Status Controller**
- [ ] **T125** Create OrderStatusController in Maliev.OrderService.Api/Controllers/OrderStatusController.cs
- [ ] **T126** Add GET /orders/{orderId}/statuses endpoint (filter internal notes for Customer role)
- [ ] **T127** Add POST /orders/{orderId}/statuses endpoint (validate state transitions, trigger notifications)

**Files Controller**
- [ ] **T128** Create OrderFilesController in Maliev.OrderService.Api/Controllers/OrderFilesController.cs
- [ ] **T129** Add GET /orders/{orderId}/files endpoint (list with file_role filtering)
- [ ] **T130** Add POST /orders/{orderId}/files endpoint (multipart upload with file_role/file_category validation)
- [ ] **T131** Add GET /orders/{orderId}/files/{fileId} endpoint (download with access control)
- [ ] **T132** Add DELETE /orders/{orderId}/files/{fileId} endpoint (soft delete)

**Notes Controller**
- [ ] **T133** Create OrderNotesController in Maliev.OrderService.Api/Controllers/OrderNotesController.cs
- [ ] **T134** Add GET /orders/{orderId}/notes endpoint (filter internal notes for Customer role)
- [ ] **T135** Add POST /orders/{orderId}/notes endpoint (RBAC enforcement: Customer cannot create internal notes)

## Phase 3.9: Middleware & Error Handling
- [ ] **T136** Create JwtAuthenticationMiddleware in Maliev.OrderService.Api/Middleware/JwtAuthenticationMiddleware.cs (Auth Service integration, extract UserContext)
- [ ] **T137** Create ExceptionHandlingMiddleware in Maliev.OrderService.Api/Middleware/ExceptionHandlingMiddleware.cs (global error handling, ErrorResponse)
- [ ] **T138** Register middleware in Program.cs (correct order: Swagger → HTTPS → RateLimiter → Auth → Authorization)

## Phase 3.10: Deployment Configuration
- [ ] **T139** Create Dockerfile for Maliev.OrderService.Api (multi-stage build, .NET 9 runtime)
- [ ] **T140** Create .dockerignore to exclude bin/, obj/, .vs/, .git/
- [ ] **T141** Create GitHub Actions workflow ci-develop.yml (.github/workflows/ci-develop.yml) for develop branch
- [ ] **T142** Create GitHub Actions workflow ci-staging.yml (.github/workflows/ci-staging.yml) for staging branch
- [ ] **T143** Create GitHub Actions workflow ci-main.yml (.github/workflows/ci-main.yml) for main branch (includes GitOps update)
- [ ] **T144** Test Docker build locally (docker build -t order-service:dev .)

## Phase 3.11: Unit Tests
**Service Unit Tests (can run in parallel)**
- [ ] **T145** [P] Unit tests for OrderService state machine in Maliev.OrderService.Tests/Unit/Services/OrderServiceStateMachineTests.cs (all 16 states, valid/invalid transitions)
- [ ] **T146** [P] Unit tests for OrderService RBAC logic in Maliev.OrderService.Tests/Unit/Services/OrderServiceRbacTests.cs (Customer/Employee/Manager/Admin filtering)
- [ ] **T147** [P] Unit tests for OrderService optimistic concurrency in Maliev.OrderService.Tests/Unit/Services/OrderServiceConcurrencyTests.cs (version conflicts)
- [ ] **T148** [P] Unit tests for OrderService batch operations in Maliev.OrderService.Tests/Unit/Services/OrderServiceBatchTests.cs (transaction rollback)
- [ ] **T149** [P] Unit tests for FileService validation in Maliev.OrderService.Tests/Unit/Services/FileServiceValidationTests.cs (size limits, file roles)
- [ ] **T150** [P] Unit tests for OrderNoteService RBAC in Maliev.OrderService.Tests/Unit/Services/OrderNoteServiceTests.cs (internal note filtering)

**Validator Unit Tests (can run in parallel)**
- [ ] **T151** [P] Unit tests for CreateOrderRequestValidator in Maliev.OrderService.Tests/Unit/Validators/CreateOrderRequestValidatorTests.cs
- [ ] **T152** [P] Unit tests for UpdateStatusRequestValidator in Maliev.OrderService.Tests/Unit/Validators/UpdateStatusRequestValidatorTests.cs (state transition rules)

## Phase 3.12: Polish & Compliance
- [ ] **T153** Performance test: Verify <200ms p95 response time for GET /orders with 1000 orders in database
- [ ] **T154** Performance test: Verify 500+ req/s throughput for order creation
- [ ] **T155** Performance test: Verify <512MB memory per pod under load
- [ ] **T156** Security audit: Scan for hardcoded secrets (connection strings, URLs, keys) in all .cs files and appsettings.json
- [ ] **T157** Security audit: Verify all environment variables use placeholders (CUSTOMER_SERVICE_URL, MATERIAL_SERVICE_URL, etc.)
- [ ] **T158** Security audit: Verify no production URLs or infrastructure topology in source code
- [ ] **T159** Clean unused artifacts: Remove any sample files, boilerplate, outdated documentation
- [ ] **T160** Verify zero build warnings: Build solution with TreatWarningsAsErrors enabled
- [ ] **T161** Verify 80%+ code coverage for business-critical logic (OrderService, FileService, state machine, RBAC)
- [ ] **T162** Test all quickstart.md scenarios manually (8 scenarios)
- [ ] **T163** Update CLAUDE.md with Order Service patterns (state machine, RBAC, external services, notes system)
- [ ] **T164** Final Constitution compliance check: All 9 principles validated

## Dependencies

**Critical Ordering:**
- **T001-T013** (Setup) before everything
- **T014-T043** (Models & DB) before T044-T067 (Tests)
- **T044-T067** (Tests) MUST be written and MUST FAIL before T068+ (Implementation)
- **T068-T084** (DTOs & Validators) before T100-T116 (Services)
- **T085-T099** (External Clients) before T100-T116 (Services)
- **T100-T116** (Services) before T117-T135 (Controllers)
- **T136-T138** (Middleware) before T141-T143 (CI/CD)
- **T145-T152** (Unit Tests) can run anytime after T100-T116
- **T153-T164** (Polish) after all implementation complete

**Blocking Dependencies:**
- T040 (DbContext) blocks T041-T043 (Migrations)
- T080 (MappingProfile) blocks T117+ (Controllers)
- T099 (HttpClient registration) blocks T100-T116 (Services)
- T101-T109 (OrderService) blocks T117-T124 (OrdersController)
- T138 (Middleware registration) blocks T141-T143 (CI/CD)

## Parallel Execution Examples

**Phase 3.2 - Entity Models (T014-T026) - Run all 13 together:**
```
Task: "Create Order entity model in Maliev.OrderService.Data/Models/Order.cs"
Task: "Create OrderStatus entity model in Maliev.OrderService.Data/Models/OrderStatus.cs"
Task: "Create OrderFile entity model in Maliev.OrderService.Data/Models/OrderFile.cs"
Task: "Create OrderNote entity model in Maliev.OrderService.Data/Models/OrderNote.cs"
Task: "Create Order3DPrintingAttributes entity model in Maliev.OrderService.Data/Models/Order3DPrintingAttributes.cs"
... (all 13 entity models)
```

**Phase 3.2 - EF Configurations (T027-T039) - Run all 13 together:**
```
Task: "Create OrderConfiguration with FluentAPI in Maliev.OrderService.Data/Configurations/OrderConfiguration.cs"
Task: "Create OrderStatusConfiguration with FluentAPI in Maliev.OrderService.Data/Configurations/OrderStatusConfiguration.cs"
... (all 13 configuration files)
```

**Phase 3.3 - Contract Tests (T044-T067) - Run all 24 together:**
```
Task: "Contract test GET /orders in Maliev.OrderService.Tests/Contract/OrderEndpointTests.cs"
Task: "Contract test POST /orders in Maliev.OrderService.Tests/Contract/OrderEndpointTests.cs"
Task: "Contract test GET /orders/{orderId}/statuses in Maliev.OrderService.Tests/Contract/StatusEndpointTests.cs"
Task: "Integration test Scenario 1: Customer creates confidential order in Maliev.OrderService.Tests/Integration/ConfidentialOrderTests.cs"
... (all 24 test files)
```

**Phase 3.4 - DTOs (T068-T079) - Run all 12 together:**
```
Task: "Create OrderDto in Maliev.OrderService.Api/Models/DTOs/OrderDto.cs"
Task: "Create CreateOrderRequest in Maliev.OrderService.Api/Models/DTOs/CreateOrderRequest.cs"
... (all 12 DTO files)
```

**Phase 3.6 - External Service Clients (T085-T098) - Run all 14 together:**
```
Task: "Create ICustomerServiceClient interface in Maliev.OrderService.Api/Services/ICustomerServiceClient.cs"
Task: "Implement CustomerServiceClient in Maliev.OrderService.Api/Services/CustomerServiceClient.cs"
... (all 7 interfaces + 7 implementations)
```

## Validation Checklist
*GATE: Verify before marking feature complete*

- [x] All contracts have corresponding tests (T044-T067 cover all endpoints)
- [x] All 13 entities have model tasks (T014-T026)
- [x] All 13 entities have configuration tasks (T027-T039)
- [x] All tests come before implementation (Phase 3.3 before Phase 3.4+)
- [x] Parallel tasks [P] are truly independent (different files)
- [x] Each task specifies exact file path
- [x] Security audit tasks included (T156-T158)
- [x] Artifact cleanup tasks included (T159)
- [x] Zero warnings verification task included (T160)
- [x] Constitution compliance check included (T164)
- [x] All 7 external service integrations implemented (T085-T099)
- [x] 16-state workflow implemented (T102)
- [x] RBAC with 4 roles implemented (T103)
- [x] Optimistic concurrency implemented (T104)
- [x] Batch operations with rollback implemented (T105)

## Notes

- **Total Tasks**: 164 tasks
- **Estimated Duration**: 12-15 days for full implementation
- **Critical Path**: Setup → Models → Tests → Services → Controllers → Polish
- **Parallel Opportunities**:
  - 13 entity models (T014-T026)
  - 13 EF configurations (T027-T039)
  - 24 contract/integration tests (T044-T067)
  - 12 DTOs (T068-T079)
  - 14 external service clients (T085-T098)
  - Total: 76 tasks can run in parallel
- **TDD Enforcement**: Tests (T044-T067) MUST fail before implementation begins
- **Zero Warnings**: T160 enforced via TreatWarningsAsErrors in .csproj
- **Constitution Compliance**: All 9 principles validated in T164

## Task Generation Metadata

**Generated from:**
- plan.md: .NET 9, PostgreSQL 18, 13 entities, 7 external services
- data-model.md: 13 entities with complete schema
- contracts/openapi.yaml: 4 endpoint groups, 20+ operations
- quickstart.md: 8 integration test scenarios
- research.md: 15 technical decisions

**Applied Rules:**
- Each entity → model task [P] + configuration task [P]
- Each endpoint → contract test [P] + implementation task
- Each integration scenario → integration test [P]
- Each external service → interface [P] + implementation [P]
- Different files → [P] parallel
- Same file → sequential
- Tests before implementation (TDD)
- Setup → Tests → Core → Integration → Polish
