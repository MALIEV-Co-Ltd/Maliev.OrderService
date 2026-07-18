# Quickstart: Using Permission-Based Authorization

## Protecting Endpoints

Use the `[RequirePermission]` attribute on controllers or actions.

```csharp
[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    [HttpPost]
    [RequirePermission(OrderPermissions.OrdersCreate)]
    public async Task<IActionResult> CreateOrder(...) { ... }

    [HttpGet("{id}")]
    [RequirePermission(OrderPermissions.OrdersRead)]
    public async Task<IActionResult> GetOrder(string id) { ... }
}
```

## Testing with Permissions

Update your test client setup to include specific permissions.

```csharp
// In your test class
var client = _factory.CreateAuthenticatedClient("test-user", 
    permissions: new[] { OrderPermissions.OrdersRead });

// Or use a predefined role
var client = _factory.CreateAuthenticatedClient("test-manager", 
    role: "order-manager");
```

## Adding New Permissions

1. Add the constant to `OrderPermissions.cs`.
2. Add a description if needed for IAM registration.
3. Update `OrderPredefinedRoles.cs` if it should be part of a default role.
4. The system will automatically register it on the next startup.
