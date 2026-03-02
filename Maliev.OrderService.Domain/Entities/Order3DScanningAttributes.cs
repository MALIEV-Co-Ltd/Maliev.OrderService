namespace Maliev.OrderService.Domain.Entities
{
    /// <summary>
    /// Represents service-specific attributes for 3D scanning orders.
    /// </summary>
    public class Order3DScanningAttributes
    {
        /// <summary>Gets or sets the order identifier (PK and FK).</summary>
        public string OrderId { get; set; } = null!;

        /// <summary>Gets or sets the required scanning accuracy specification.</summary>
        public string? RequiredAccuracy { get; set; }

        /// <summary>Gets or sets the scan location (null means in-house).</summary>
        public string? ScanLocation { get; set; }

        /// <summary>Gets or sets the desired output file formats as CSV (e.g., "STL,STEP,PLY").</summary>
        public string? OutputFileFormats { get; set; }

        /// <summary>Gets or sets a value indicating whether a deviation report is requested.</summary>
        public bool DeviationReportRequested { get; set; }

        // Navigation Properties
        /// <summary>Gets or sets the parent order navigation property.</summary>
        public Order Order { get; set; } = null!;
    }
}
