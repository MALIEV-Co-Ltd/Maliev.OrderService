namespace Maliev.OrderService.Api.Authorization
{
    /// <summary>
    /// Defines all permissions for the Order Service.
    /// Permissions follow the format: order.{resource}.{action}
    /// </summary>
    public static class OrderPermissions
    {
        // Order Operations
        /// <summary>Permission to create new orders.</summary>
        public const string OrdersCreate = "order.orders.create";
        /// <summary>Permission to read order details.</summary>
        public const string OrdersRead = "order.orders.read";
        /// <summary>Permission to update order information.</summary>
        public const string OrdersUpdate = "order.orders.update";
        /// <summary>Permission to delete orders.</summary>
        public const string OrdersDelete = "order.orders.delete";
        /// <summary>Permission to approve orders.</summary>
        public const string OrdersApprove = "order.orders.approve";
        /// <summary>Permission to cancel orders.</summary>
        public const string OrdersCancel = "order.orders.cancel";
        /// <summary>Permission to fulfill/complete orders.</summary>
        public const string OrdersFulfill = "order.orders.fulfill";
        /// <summary>Permission to export orders.</summary>
        public const string OrdersExport = "order.orders.export";

        // Line Item Operations
        /// <summary>Permission to create order line items.</summary>
        public const string LineItemsCreate = "order.line-items.create";
        /// <summary>Permission to read line item details.</summary>
        public const string LineItemsRead = "order.line-items.read";
        /// <summary>Permission to update line items.</summary>
        public const string LineItemsUpdate = "order.line-items.update";
        /// <summary>Permission to delete line items.</summary>
        public const string LineItemsDelete = "order.line-items.delete";

        // Reporting Operations
        /// <summary>Permission to view sales reports.</summary>
        public const string ReportsSales = "order.reports.sales";
        /// <summary>Permission to access order analytics.</summary>
        public const string ReportsAnalytics = "order.reports.analytics";
        /// <summary>Permission to export reports.</summary>
        public const string ReportsExport = "order.reports.export";

        /// <summary>
        /// Collection of all defined order permissions with descriptions.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> AllWithDescriptions = new Dictionary<string, string>
    {
        { OrdersCreate, "Create new orders" },
        { OrdersRead, "Read order details" },
        { OrdersUpdate, "Update order information" },
        { OrdersDelete, "Delete orders" },
        { OrdersApprove, "Approve orders for production" },
        { OrdersCancel, "Cancel orders" },
        { OrdersFulfill, "Fulfill/complete orders" },
        { OrdersExport, "Export order data" },
        { LineItemsCreate, "Add items to an order" },
        { LineItemsRead, "View item details" },
        { LineItemsUpdate, "Modify item specifications" },
        { LineItemsDelete, "Remove items from an order" },
        { ReportsSales, "View sales performance reports" },
        { ReportsAnalytics, "Access detailed order analytics" },
        { ReportsExport, "Export report data" }
    };

        /// <summary>
        /// All permissions defined for the Order Service.
        /// </summary>
        public static readonly string[] All = [.. AllWithDescriptions.Keys];
    }
}
