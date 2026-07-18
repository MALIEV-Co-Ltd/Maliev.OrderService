namespace Maliev.OrderService.Application.Authorization;

/// <summary>
/// Provides access to predefined roles for the Order Service.
/// </summary>
public static class OrderPredefinedRoles
{
    public const string Admin = "roles.order.admin";
    public const string Sales = "roles.order.sales";
    public const string Operations = "roles.order.operations";
    public const string Viewer = "roles.order.viewer";

    public static readonly IReadOnlyList<(string RoleId, string Description, string[] Permissions)> All = new List<(string, string, string[])>
    {
        (
            Admin,
            "Order Administrator with full access",
            new[]
            {
                OrderPermissions.OrderCreate,
                OrderPermissions.OrderRead,
                OrderPermissions.OrderUpdate,
                OrderPermissions.OrderDelete,
                OrderPermissions.OrderApprove,
                OrderPermissions.OrderCancel,
                OrderPermissions.OrderFulfill,
                OrderPermissions.OrderExport,
                OrderPermissions.LineItemCreate,
                OrderPermissions.LineItemRead,
                OrderPermissions.LineItemUpdate,
                OrderPermissions.LineItemDelete,
                OrderPermissions.ReportSales,
                OrderPermissions.ReportAnalytics,
                OrderPermissions.ReportExport,
            }
        ),
        (
            Sales,
            "Order Sales role with create and read access",
            new[]
            {
                OrderPermissions.OrderCreate,
                OrderPermissions.OrderRead,
                OrderPermissions.OrderUpdate,
                OrderPermissions.LineItemCreate,
                OrderPermissions.LineItemRead,
                OrderPermissions.LineItemUpdate,
                OrderPermissions.ReportSales,
            }
        ),
        (
            Operations,
            "Order Operations role with fulfill and update access",
            new[]
            {
                OrderPermissions.OrderRead,
                OrderPermissions.OrderUpdate,
                OrderPermissions.OrderApprove,
                OrderPermissions.OrderCancel,
                OrderPermissions.OrderFulfill,
                OrderPermissions.LineItemRead,
                OrderPermissions.LineItemUpdate,
                OrderPermissions.ReportAnalytics,
            }
        ),
        (
            Viewer,
            "Order Viewer with read-only access",
            new[]
            {
                OrderPermissions.OrderRead,
                OrderPermissions.LineItemRead,
                OrderPermissions.ReportSales,
                OrderPermissions.ReportAnalytics,
            }
        ),
    };
}
