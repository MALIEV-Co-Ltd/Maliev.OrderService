# Feature Specification: Permission-Based Authorization Migration

**Feature Branch**: `002-iam-integration`  
**Created**: 2025-12-22  
**Status**: Draft  
**Input**: User description: "Migrate OrderService from policy-based authorization to fine-grained permission-based authorization using the IAM service."

## Clarifications

### Session 2025-12-22
- Q: How long should the OrderService cache a user's permissions before re-validating with the IAM service? → A: Short-term (5-10 Minutes)
- Q: What should happen if a role already exists in IAM with different permissions during startup registration? → A: Overwrite (Sync) - The code is the source of truth.
- Q: How should the API respond if a user includes fields they aren't permitted to change in an update request? → A: Reject Entire Request (403 Forbidden) - No partial updates.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Full Order Administration (Priority: P1)

As an **Administrator**, I want to have unrestricted access to all order-related operations so that I can manage the entire system, override any issues, and perform audits.

**Why this priority**: High value for system maintenance and troubleshooting. Ensures that at least one role can perform any action.

**Independent Test**: Can be tested by assigning the `order-admin` role to a user and verifying they can successfully call every endpoint in the OrderService.

**Acceptance Scenarios**:

1. **Given** a user with the `order-admin` role, **When** they attempt to create, read, update, delete, approve, cancel, fulfill, or export orders, **Then** all operations succeed.
2. **Given** a user with the `order-admin` role, **When** they access sales and analytics reports, **Then** the data is retrieved successfully.

---

### User Story 2 - Order Management (Priority: P1)

As an **Order Manager**, I want to create and process orders (approval and fulfillment) without having full administrative rights (like deleting orders), so that I can handle the daily operations of the business.

**Why this priority**: Core business workflow. Necessary for the operational team to process orders.

**Independent Test**: Can be tested by assigning `order-manager` role and verifying creation/processing works while deletion fails.

**Acceptance Scenarios**:

1. **Given** a user with the `order-manager` role, **When** they create, approve, or fulfill an order, **Then** the operation succeeds.
2. **Given** a user with the `order-manager` role, **When** they attempt to delete an order, **Then** the operation is denied with a "Forbidden" response.

---

### User Story 3 - Order Creation and Self-Management (Priority: P2)

As an **Order Creator**, I want to create orders and manage the ones I've created so that I can track my own requests.

**Why this priority**: Essential for individual users or customers who need to place orders but shouldn't see or modify others' orders.

**Independent Test**: Can be tested by verifying a user can manage their own created orders but receives an error when accessing an order created by another user.

**Acceptance Scenarios**:

1. **Given** a user with the `order-creator` role, **When** they create a new order, **Then** they can subsequently read and update that specific order.
2. **Given** a user with the `order-creator` role, **When** they attempt to read an order they did not create, **Then** the system denies access. (Visibility is restricted to owned records for this role).

---

### User Story 4 - Fulfillment Operations (Priority: P2)

As a **Fulfillment Staff Member**, I want to see order details and mark them as fulfilled or cancelled so that I can update the status of physical deliveries.

**Why this priority**: Critical for the logistics part of the order lifecycle.

**Independent Test**: Can be tested by verifying the user can update order statuses but cannot change core order details (like items or prices).

**Acceptance Scenarios**:

1. **Given** a user with the `order-fulfillment` role, **When** they mark an order as fulfilled or cancelled, **Then** the status update succeeds.
2. **Given** a user with the `order-fulfillment` role, **When** they attempt to update order pricing or add new line items, **Then** the operation is denied.

### Edge Cases

- **Multiple Roles**: If a user has both `order-creator` and `order-manager`, the most permissive role (`order-manager`) takes precedence, allowing them to see all orders.
- **Orphaned Orders**: Orders without a valid owner ID are only accessible by `order-admin` and `order-manager`.
- **IAM Downtime**: Authorization checks should fail-safe (deny access) if the IAM service is unreachable, with appropriate logging for troubleshooting.
- **Permission Caching**: Permissions are cached for 5-10 minutes to meet latency targets; changes in IAM may not be immediate.
- **Unauthorized Field Updates**: If an update request contains fields the user is not authorized to change (e.g., price change by a fulfillment user), the entire request MUST be rejected with a `403 Forbidden` response. No partial updates are allowed.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST use a permission string format of `order.{resource}.{action}` for all authorization checks.
- **FR-002**: System MUST register all defined permissions with the external IAM service upon startup, ensuring any existing definitions in IAM are synchronized/overwritten to match the code.
- **FR-003**: System MUST define and register the following roles with their associated permissions: `roles.order.admin`, `roles.order.manager`, `roles.order.creator`, `roles.order.viewer`, and `roles.order.fulfillment`. Predefined roles in IAM MUST be updated to match these sets of permissions on startup.
- **FR-004**: System MUST enforce permission checks on all API endpoints, replacing the current policy-based checks.
- **FR-005**: System MUST provide a mechanism to define permissions for Orders, Line Items, and Reporting resources independently using the `order.{resource}.{action}` format.
- **FR-006**: System MUST support the following actions for the `orders` resource: create, read, update, delete, approve, cancel, fulfill, and export.
- FR-007: System MUST support the following actions for the `line-items` resource: create, read, update, and delete.
- FR-008: System MUST support the following actions for the `reports` resource: sales, analytics, and export.
- FR-009: System MUST log all authorization failures (403 Forbidden) as structured audit events, including UserId, Resource, Action, and required permissions.
- FR-010: System MUST expose business metrics for authorization (Principle XII), including check latency, success/failure counts, and cache hit rates.

### Key Entities *(include if feature involves data)*

- **Permission**: A unique string identifier representing an action on a resource (e.g., `order.orders.create`).
- **Role**: A collection of permissions that can be assigned to users, identified using the standard format `roles.order.{role-name}` (e.g., `roles.order.manager`).
- **IAM Registration**: The process of synchronizing local permission and role definitions with the central Identity and Access Management service.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of OrderService API endpoints are protected by at least one specific permission requirement.
- **SC-002**: All 5 predefined roles are correctly registered in the IAM service with the specified permission sets.
- **SC-003**: System startup time remains within 5 seconds even with IAM permission registration overhead.
- **SC-004**: Authorization check latency is less than 50ms per request (achieved via local caching).
- **SC-005**: 0 security regressions identified after migration from policy-based to permission-based authorization.
