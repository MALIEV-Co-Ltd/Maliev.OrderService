using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.OrderService.Api.Authorization;

namespace Maliev.OrderService.Api.Services;

/// <summary>
/// Service that registers Order Service permissions and roles with the central IAM service on startup.
/// Inherits from the standard <see cref="IAMRegistrationService"/> from ServiceDefaults.
/// </summary>
public class OrderIAMRegistrationService : IAMRegistrationService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrderIAMRegistrationService"/> class.
    /// </summary>
    public OrderIAMRegistrationService(
        IHttpClientFactory httpClientFactory,
        ILogger<OrderIAMRegistrationService> logger)
        : base(httpClientFactory, logger, "order")
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<PermissionRegistration> GetPermissions()
    {
        return OrderPermissions.All.Select(p => new PermissionRegistration
        {
            PermissionId = p,
            Description = GetPermissionDescription(p)
        });
    }

    /// <inheritdoc />
    protected override IEnumerable<RoleRegistration> GetPredefinedRoles()
    {
        return OrderPredefinedRoles.All;
    }

    private static string GetPermissionDescription(string permission)
    {
        return permission switch
        {
            OrderPermissions.OrdersCreate => "Create new orders",
            OrderPermissions.OrdersRead => "Read order details",
            OrderPermissions.OrdersUpdate => "Update order information",
            OrderPermissions.OrdersDelete => "Delete orders",
            OrderPermissions.OrdersApprove => "Approve orders for production",
            OrderPermissions.OrdersCancel => "Cancel orders",
            OrderPermissions.OrdersFulfill => "Fulfill/complete orders",
            OrderPermissions.OrdersExport => "Export order data",
            OrderPermissions.LineItemsCreate => "Add items to an order",
            OrderPermissions.LineItemsRead => "View item details",
            OrderPermissions.LineItemsUpdate => "Modify item specifications",
            OrderPermissions.LineItemsDelete => "Remove items from an order",
            OrderPermissions.ReportsSales => "View sales performance reports",
            OrderPermissions.ReportsAnalytics => "Access detailed order analytics",
            OrderPermissions.ReportsExport => "Export report data",
            _ => $"Permission: {permission}"
        };
    }
}
