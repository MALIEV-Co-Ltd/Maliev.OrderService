namespace Maliev.OrderService.Data.Models;

public class Order3DScanningAttributes
{
    public string OrderId { get; set; } = null!; // PK and FK
    public string? RequiredAccuracy { get; set; }
    public string? ScanLocation { get; set; } // null = in-house
    public string? OutputFileFormats { get; set; } // CSV: "STL,STEP,PLY"
    public bool DeviationReportRequested { get; set; }

    // Navigation Properties
    public Order Order { get; set; } = null!;
}
