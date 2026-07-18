# Tasks: Permission-Based Authorization Migration

**Input**: Design documents from `/specs/002-iam-integration/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic authorization constants

- [X] T001 [P] Create permission constants in `Maliev.OrderService.Api/Authorization/OrderPermissions.cs`
- [X] T002 [P] Create predefined role mappings (`roles.order.*`) in `Maliev.OrderService.Api/Authorization/OrderPredefinedRoles.cs`
- [X] T003 [P] Implement `[RequirePermission]` attribute in `Maliev.OrderService.Api/Authorization/RequirePermissionAttribute.cs`
- [X] T004 Define IAM service client configuration using `builder.Services.AddIAMClient()` from `ServiceDefaults` in `Maliev.OrderService.Api/Program.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure for IAM integration and permission enforcement

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T005 Implement `IamServiceClient.cs` wrapper (if needed) or use `IAMService` client from `ServiceDefaults` in `Maliev.OrderService.Api/Services/External/`
- [X] T006 [P] Implement `PermissionAuthorizationPolicyProvider.cs` in `Maliev.OrderService.Api/Authorization/PermissionAuthorizationPolicyProvider.cs`
- [X] T007 [P] Implement `PermissionAuthorizationHandler.cs` with Redis caching in `Maliev.OrderService.Api/Authorization/PermissionAuthorizationHandler.cs`
- [X] T007.1 [P] Implement structured audit logging for 403 Forbidden responses in `PermissionAuthorizationHandler.cs`
- [X] T007.2 [P] Implement OpenTelemetry metrics (latencies, success/fail counts) in `PermissionAuthorizationHandler.cs`
- [X] T008 Implement `OrderIAMRegistrationService.cs` inheriting from `Maliev.Aspire.ServiceDefaults.IAM.IAMRegistrationService`
- [X] T009 Register all authorization services and the hosted registration service in `Maliev.OrderService.Api/Program.cs`
- [X] T010 Update `Maliev.OrderService.Tests/Testing/BaseIntegrationTestFactory.cs` to support permission-based token generation

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Full Order Administration (Priority: P1) 🎯 MVP

**Goal**: Enable administrators to perform all operations, including sensitive ones like deletion and reporting.

**Independent Test**: Verify that a user with the `roles.order.admin` role can access all endpoints, specifically `DELETE` and reporting endpoints.

### Tests for User Story 1

- [X] T011 [P] [US1] Create integration test for Admin full access in `Maliev.OrderService.Tests/Contract/AdminAccessTests.cs`

### Implementation for User Story 1

- [X] T012 [US1] Update `OrdersController.cs` with `[RequirePermission(OrderPermissions.OrdersDelete)]` and `[RequirePermission(OrderPermissions.OrdersExport)]`
- [X] T013 [US1] Create `ReportsController.cs` in `Maliev.OrderService.Api/Controllers/` with `order.reports.*` permissions
- [X] T014 [US1] Add admin permissions to all other controller actions via `[RequirePermission]`

**Checkpoint**: User Story 1 (Admin) is fully functional and testable independently.

---

## Phase 4: User Story 2 - Order Management (Priority: P1)

**Goal**: Enable managers (roles.order.manager) to create, approve, and fulfill orders without administrative rights like deletion.

**Independent Test**: Verify that a user with the `roles.order.manager` role can create/approve orders but receives `403 Forbidden` on deletion.

### Tests for User Story 2

- [X] T015 [P] [US2] Create integration test for Manager operational access in `Maliev.OrderService.Tests/Contract/ManagerAccessTests.cs`

### Implementation for User Story 2

- [X] T016 [US2] Update `OrdersController.cs` and `BatchOrdersController.cs` with manager-level permissions
- [X] T017 [US2] Update `OrderStatusController.cs` with `order.orders.approve` and `order.orders.fulfill` permissions

**Checkpoint**: User Story 2 (Manager) is fully functional and testable independently.

---

## Phase 5: User Story 3 - Order Creation and Self-Management (Priority: P2)

**Goal**: Enable creators to manage only their own orders with restricted global visibility.

**Independent Test**: Verify a creator can read their own order but gets `403 Forbidden` when accessing an order they don't own.

### Tests for User Story 3

- [X] T018 [P] [US3] Create integration test for Creator ownership logic in `Maliev.OrderService.Tests/Contract/CreatorOwnershipTests.cs`

### Implementation for User Story 3

- [X] T019 [US3] Implement ownership-aware visibility logic in `PermissionAuthorizationHandler.cs` (Option C: Role-Based Behavior)
- [X] T020 [US3] Update `OrdersController.cs`, `OrderFilesController.cs`, and `OrderNotesController.cs` with creator permissions

**Checkpoint**: User Story 3 (Creator) is fully functional and testable independently.

---

## Phase 6: User Story 4 - Fulfillment Operations (Priority: P2)

**Goal**: Enable fulfillment staff to update statuses while preventing unauthorized field changes like pricing.

**Independent Test**: Verify fulfillment staff can mark orders as fulfilled but receive `403 Forbidden` if they try to update order prices.

### Tests for User Story 4

- [X] T021 [P] [US4] Create integration test for Fulfillment access and field-level restrictions in `Maliev.OrderService.Tests/Contract/FulfillmentAccessTests.cs`

### Implementation for User Story 4

- [X] T022 [US4] Update `OrderStatusController.cs` with `order.orders.fulfill` and `order.orders.cancel` permissions
- [X] T023 [US4] Implement field-level authorization check in `OrdersController.cs` (or via a decorator) to reject pricing updates for non-admins/managers

**Checkpoint**: All user stories are now independently functional.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final cleanups, performance validation, and security hardening

- [X] T024 [P] Verify SC-004 (Authorization check latency < 50ms) via performance tests
- [X] T025 [P] Verify SC-003 (Startup time < 5s) via logs
- [X] T026 Final code refactoring and XML documentation updates
- [X] T027 Run and validate all scenarios in quickstart.md
- [X] T028 Verify metrics endpoint /metrics exposes authorization counters (Principle XII)
- [X] T029 Execute full service regression test suite to ensure zero regressions (SC-005)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-6)**: All depend on Foundational phase completion
  - Can proceed sequentially (US1 → US2 → US3 → US4) or in parallel if staff allows.
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 & 2**: High priority, can be done together or sequentially.
- **User Story 3**: Depends on `PermissionAuthorizationHandler` enhancements for ownership logic.
- **User Story 4**: Depends on foundational status update endpoints being protected.

### Parallel Opportunities

- T001, T002, T003 can be implemented in parallel.
- T006, T007 can be implemented in parallel after T003/T004.
- Integration tests (T011, T015, T018, T021) can be drafted in parallel before implementation.

---

## Implementation Strategy

### MVP First (User Story 1 & 2)

1. Complete Phase 1 & 2 (Core Infrastructure).
2. Complete Phase 3 (Admin) and Phase 4 (Manager).
3. **STOP and VALIDATE**: Verify the core business workflow is protected and functional.

### Incremental Delivery

1. Foundation ready.
2. Admin/Manager access live (MVP).
3. Add Ownership logic (Creator).
4. Add Field-level restrictions (Fulfillment).

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Verify tests fail before implementing
- Commits should be made after each task completion
