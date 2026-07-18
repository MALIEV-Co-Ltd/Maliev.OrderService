# Research: Permission-Based Authorization Migration

## Permission Definitions

All permissions follow the format `order.{resource}.{action}`.

### Order Operations (`order.orders.*`)
- `order.orders.create`: Create new orders
- `order.orders.read`: Read order details (visibility scope determined by role)
- `order.orders.update`: Update order information
- `order.orders.delete`: Delete orders (Admin only)
- `order.orders.approve`: Approve orders for production
- `order.orders.cancel`: Cancel orders
- `order.orders.fulfill`: Mark orders as fulfilled/completed
- `order.orders.export`: Export order data

### Line Item Operations (`order.line-items.*`)
- `order.line-items.create`: Add items to an order
- `order.line-items.read`: View item details
- `order.line-items.update`: Modify item specifications
- `order.line-items.delete`: Remove items from an order

### Reporting Operations (`order.reports.*`)
- `order.reports.sales`: View sales performance reports
- `order.reports.analytics`: Access detailed order analytics
- `order.reports.export`: Export report data

## Predefined Role Mappings

| Role | Permissions | Description |
|------|-------------|-------------|
| `roles.order.admin` | `order.*` | Full access to all operations |
| `roles.order.manager` | `order.orders.{create,read,update,approve,fulfill,export}`, `order.line-items.*`, `order.reports.*` | Full operational and reporting access |
| `roles.order.creator` | `order.orders.{create,read,update,cancel}`, `order.line-items.*` | Can manage their own orders |
| `roles.order.viewer` | `order.orders.read`, `order.line-items.read`, `order.reports.sales` | Read-only access to orders and sales reports |
| `roles.order.fulfillment` | `order.orders.{read,fulfill,cancel}`, `order.line-items.read` | Focused on processing and delivery |

## IAM Integration Strategy

### Registration Flow
1. **Startup**: `OrderIAMRegistrationService` (inheriting from `IAMRegistrationService`) starts.
2. **Discovery**: Service discovers all constants in `OrderPermissions`.
3. **Sync Permissions**: Calls `POST /iam/v1/permissions/register` with the list of permissions.
   - Strategy: Overwrite/Sync (IAM becomes source of truth for active permissions).
4. **Sync Roles**: Calls `POST /iam/v1/roles/register` with the `OrderPredefinedRoles` mapping.
   - Strategy: Overwrite (Sync).

### Authorization Flow
1. **Request**: User sends request with JWT.
2. **Middleware**: `PermissionAuthorizationHandler` is triggered by `[RequirePermission]`.
3. **Cache Check**: Check local Redis/Memory cache for `user_permissions:{userId}`.
4. **IAM Call (Cache Miss)**: If miss, call `GET /iam/v1/users/{userId}/permissions`.
5. **Cache Set**: Store result in cache with 5-10m TTL.
6. **Enforcement**: Check if required permission is in user's permission set.

## Proposed Components

### `OrderPermissions` (Constants)
```csharp
public static class OrderPermissions
{
    public const string OrdersCreate = "order.orders.create";
    // ... others
}
```

### `RequirePermissionAttribute`
```csharp
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission) : base(policy: permission) { }
}
```

### `PermissionAuthorizationPolicyProvider`
Dynamically creates policies for each permission string if they don't exist.

### `PermissionAuthorizationHandler`
Handles the logic of checking the user's permissions (with caching).
