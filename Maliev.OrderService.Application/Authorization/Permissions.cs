namespace Maliev.OrderService.Application.Authorization;

/// <summary>
/// Defines the permissions for the Order Service.
/// </summary>
public static class OrderPermissions
{
    public const string OrderCreate = "order.orders.create";
    public const string OrderRead = "order.orders.read";
    public const string OrderUpdate = "order.orders.update";
    public const string OrderDelete = "order.orders.delete";
    public const string OrderApprove = "order.orders.approve";
    public const string OrderCancel = "order.orders.cancel";
    public const string OrderFulfill = "order.orders.fulfill";
    public const string OrderExport = "order.orders.export";

    public const string LineItemCreate = "order.line-items.create";
    public const string LineItemRead = "order.line-items.read";
    public const string LineItemUpdate = "order.line-items.update";
    public const string LineItemDelete = "order.line-items.delete";

    public const string ReportSales = "order.reports.sales";
    public const string ReportAnalytics = "order.reports.analytics";
    public const string ReportExport = "order.reports.export";

    public static readonly IReadOnlyDictionary<string, string> AllWithDescriptions = new Dictionary<string, string>
    {
        { OrderCreate, "Create orders" },
        { OrderRead, "Read orders" },
        { OrderUpdate, "Update orders" },
        { OrderDelete, "Delete orders" },
        { OrderApprove, "Approve orders" },
        { OrderCancel, "Cancel orders" },
        { OrderFulfill, "Fulfill orders" },
        { OrderExport, "Export orders" },
        { LineItemCreate, "Create line items" },
        { LineItemRead, "Read line items" },
        { LineItemUpdate, "Update line items" },
        { LineItemDelete, "Delete line items" },
        { ReportSales, "Generate sales reports" },
        { ReportAnalytics, "Generate analytics reports" },
        { ReportExport, "Export reports" },
    };

    public static string[] All => AllWithDescriptions.Keys.ToArray();
}
