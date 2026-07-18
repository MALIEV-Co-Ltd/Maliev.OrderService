# IAM Service Contract

## Permission Registration API

`POST /iam/v1/permissions/register`

Registers or updates the service's permissions.

**Request Body**:
```json
{
  "serviceName": "OrderService",
  "permissions": [
    { "permissionId": "order.orders.create", "description": "Create orders" },
    { "permissionId": "order.orders.read", "description": "Read orders" }
  ]
}
```

**Response**: `200 OK`

---

## Role Registration API

`POST /iam/v1/roles/register`

Registers or updates built-in roles for the service.

**Request Body**:
```json
{
  "serviceName": "OrderService",
  "roles": [
    {
      "roleId": "roles.order.manager",
      "roleName": "Order Manager",
      "description": "Manage orders",
      "permissionIds": ["order.orders.create", "order.orders.read"]
    }
  ]
}
```

**Response**: `200 OK`

---

## User Permissions API

`GET /iam/v1/users/{userId}/permissions`

Retrieves the list of active permissions for a user across all roles.

**Response**:
```json
{
  "userId": "EMP-001",
  "permissions": [
    "order.orders.create",
    "order.orders.read",
    "order.reports.sales"
  ]
}
```
