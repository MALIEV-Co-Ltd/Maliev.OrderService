# Data Model: Authorization Entities

## Permission Entity (External/IAM)

| Field | Type | Description |
|-------|------|-------------|
| Name | String | Canonical permission name (e.g., `order.orders.create`) |
| Description | String | Human-readable description |
| Resource | String | Target resource (e.g., `orders`) |
| Action | String | Action type (e.g., `create`) |

## Role Entity (External/IAM)

| Field | Type | Description |
|-------|------|-------------|
| Name | String | Role name (e.g., `order-manager`) |
| Description | String | Role description |
| Permissions | List<String> | List of permission names assigned to this role |

## Local Cache Model (Redis)

**Key**: `user_permissions:{userId}`
**Type**: Set or Json
**Value**: `["order.orders.create", "order.orders.read", ...]`
**TTL**: 300 - 600 seconds (5-10 minutes)

## IAM Registration Request (DTO)

```json
{
  "serviceName": "OrderService",
  "permissions": [
    {
      "name": "order.orders.create",
      "description": "Create new orders"
    }
  ],
  "roles": [
    {
      "name": "order-manager",
      "permissions": ["order.orders.create", "order.orders.read"]
    }
  ]
}
```
