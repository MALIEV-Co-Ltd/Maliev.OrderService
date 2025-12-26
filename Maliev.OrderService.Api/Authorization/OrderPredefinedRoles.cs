using Maliev.Aspire.ServiceDefaults.IAM;

namespace Maliev.OrderService.Api.Authorization;

/// <summary>
/// Defines predefined roles for the Order Service.
/// Roles follow the GCP format: roles.order.{role-name}
/// </summary>
public static class OrderPredefinedRoles
{
    /// <summary>Full access to all order operations.</summary>
    public static readonly RoleRegistration Admin = new()
    {
        RoleId = "roles.order.admin",
        Description = "Full administrative access to all order operations",
        PermissionIds = OrderPermissions.All.ToList()
    };

    /// <summary>Operational access to manage orders.</summary>
    public static readonly RoleRegistration Manager = new()
    {
        RoleId = "roles.order.manager",
        Description = "Can create, update, approve, and fulfill orders",
        PermissionIds = new List<string>
        {
            OrderPermissions.OrdersCreate,
            OrderPermissions.OrdersRead,
            OrderPermissions.OrdersUpdate,
            OrderPermissions.OrdersApprove,
            OrderPermissions.OrdersCancel,
            OrderPermissions.OrdersFulfill,
            OrderPermissions.OrdersExport,
            OrderPermissions.LineItemsCreate,
            OrderPermissions.LineItemsRead,
            OrderPermissions.LineItemsUpdate,
            OrderPermissions.LineItemsDelete,
            OrderPermissions.ReportsSales,
            OrderPermissions.ReportsAnalytics,
            OrderPermissions.ReportsExport
        }
    };

    /// <summary>Can create and manage own orders.</summary>
    public static readonly RoleRegistration Creator = new()
    {
        RoleId = "roles.order.creator",
        Description = "Can create and manage own orders",
        PermissionIds = new List<string>
        {
            OrderPermissions.OrdersCreate,
            OrderPermissions.OrdersRead,
            OrderPermissions.OrdersUpdate,
            OrderPermissions.OrdersCancel,
            OrderPermissions.LineItemsCreate,
            OrderPermissions.LineItemsRead,
            OrderPermissions.LineItemsUpdate,
            OrderPermissions.LineItemsDelete
        }
    };

    /// <summary>Read-only access to orders.</summary>
    public static readonly RoleRegistration Viewer = new()
    {
        RoleId = "roles.order.viewer",
        Description = "Read-only access to orders and sales reports",
        PermissionIds = new List<string>
        {
            OrderPermissions.OrdersRead,
            OrderPermissions.LineItemsRead,
            OrderPermissions.ReportsSales
        }
    };

    /// <summary>Focused on processing and delivery.</summary>
    public static readonly RoleRegistration Fulfillment = new()
    {
        RoleId = "roles.order.fulfillment",
        Description = "Can fulfill and cancel orders",
        PermissionIds = new List<string>
        {
            OrderPermissions.OrdersRead,
            OrderPermissions.OrdersFulfill,
            OrderPermissions.OrdersCancel,
            OrderPermissions.LineItemsRead
        }
    };

    /// <summary>
    /// All predefined roles for the Order Service.
    /// </summary>
    public static readonly RoleRegistration[] All = new[]
    {
        Admin, Manager, Creator, Viewer, Fulfillment
    };
}
