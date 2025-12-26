# OrderService Specification - Permission-Based Authorization Migration

## Overview
Migrate OrderService from policy-based authorization to fine-grained permission-based authorization using the IAM service.

## Current State
- Uses policy-based authorization
- No fine-grained permission control

## Target State
- Permission-based authorization with format: `order.{resource}.{action}`
- Fine-grained permissions for all operations
- Authorization checks: `[RequirePermission(OrderPermissions.OrdersCreate)]`

## Permissions to Define

### Order Operations
```
order.orders.create          - Create new orders
order.orders.read            - Read order details
order.orders.update          - Update order information
order.orders.delete          - Delete orders
order.orders.approve         - Approve orders
order.orders.cancel          - Cancel orders
order.orders.fulfill         - Fulfill/complete orders
order.orders.export          - Export orders to various formats
```

### Line Item Operations
```
order.line-items.create      - Create order line items
order.line-items.read        - Read line item details
order.line-items.update      - Update line items
order.line-items.delete      - Delete line items
```

### Reporting Operations
```
order.reports.sales          - View sales reports
order.reports.analytics      - Access order analytics
order.reports.export         - Export reports
```

## Predefined Roles

### order-admin
**Description**: Full administrative access to all order operations
**Permissions**: All order.* permissions

### order-manager
**Description**: Can create, update, approve, and fulfill orders
**Permissions**:
- order.orders.create
- order.orders.read
- order.orders.update
- order.orders.approve
- order.orders.fulfill
- order.line-items.create
- order.line-items.read
- order.line-items.update
- order.reports.sales
- order.reports.analytics

### order-creator
**Description**: Can create and manage own orders
**Permissions**:
- order.orders.create
- order.orders.read
- order.orders.update
- order.line-items.create
- order.line-items.read
- order.line-items.update

### order-viewer
**Description**: Read-only access to orders
**Permissions**:
- order.orders.read
- order.line-items.read
- order.reports.sales

### order-fulfillment
**Description**: Can fulfill and cancel orders
**Permissions**:
- order.orders.read
- order.orders.fulfill
- order.orders.cancel
- order.line-items.read

## Implementation Files

**Create**:
- `Maliev.OrderService.Api/Authorization/OrderPermissions.cs`
- `Maliev.OrderService.Api/Authorization/OrderPredefinedRoles.cs`
- `Maliev.OrderService.Api/Services/OrderIAMRegistrationService.cs`

**Update**:
- All controller files - replace `[Authorize(Policy)]` with `[RequirePermission(OrderPermissions.Xxx)]`
- `Program.cs` - add IAM client and registration service
- All integration tests - use `.WithTestAuth(OrderPermissions.Xxx)`

## Success Criteria
- [ ] ~15 permissions registered with IAM
- [ ] 5 predefined roles registered
- [ ] All endpoints have [RequirePermission]
- [ ] All tests pass
