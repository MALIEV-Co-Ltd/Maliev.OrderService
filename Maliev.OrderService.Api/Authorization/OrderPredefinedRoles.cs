namespace Maliev.OrderService.Api.Authorization
{
    /// <summary>
    /// Defines predefined roles for the Order Service.
    /// Roles follow the GCP format: roles.order.{role-name}
    /// </summary>
    public static class OrderPredefinedRoles
    {
        /// <summary>Full administrative access to all order operations.</summary>
        public const string Admin = "roles.order.admin";
        /// <summary>Operational access to manage orders.</summary>
        public const string Manager = "roles.order.manager";
        /// <summary>Can create and manage own orders.</summary>
        public const string Creator = "roles.order.creator";
        /// <summary>Read-only access to orders.</summary>
        public const string Viewer = "roles.order.viewer";
        /// <summary>Focused on processing and delivery.</summary>
        public const string Fulfillment = "roles.order.fulfillment";

        /// <summary>
        /// Collection of all predefined roles for the Order Service.
        /// </summary>
        public static readonly IReadOnlyList<(string RoleId, string Description, string[] Permissions)> All = new List<(string, string, string[])>
        {
            (Admin, "Full administrative access to all order operations", OrderPermissions.All),

            (Manager, "Can create, update, approve, and fulfill orders", new[]
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
            }),

            (Creator, "Can create and manage own orders", new[]
            {
                OrderPermissions.OrdersCreate,
                OrderPermissions.OrdersRead,
                OrderPermissions.OrdersUpdate,
                OrderPermissions.OrdersCancel,
                OrderPermissions.LineItemsCreate,
                OrderPermissions.LineItemsRead,
                OrderPermissions.LineItemsUpdate,
                OrderPermissions.LineItemsDelete
            }),

            (Viewer, "Read-only access to orders and sales reports", new[]
            {
                OrderPermissions.OrdersRead,
                OrderPermissions.LineItemsRead,
                OrderPermissions.ReportsSales
            }),

            (Fulfillment, "Can fulfill and cancel orders", new[]
            {
                OrderPermissions.OrdersRead,
                OrderPermissions.OrdersFulfill,
                OrderPermissions.OrdersCancel,
                OrderPermissions.LineItemsRead
            })
        };
    }
}
