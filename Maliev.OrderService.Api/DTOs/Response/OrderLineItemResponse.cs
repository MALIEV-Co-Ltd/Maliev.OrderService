namespace Maliev.OrderService.Api.DTOs.Response
{
    /// <summary>
    /// Response model for a production line item derived from an order.
    /// </summary>
    public class OrderLineItemResponse
    {
        /// <summary>Gets or sets the stable line item identifier.</summary>
        public required Guid OrderItemId { get; set; }

        /// <summary>Gets or sets the originating project identifier, when known.</summary>
        public Guid? SourceProjectId { get; set; }

        /// <summary>Gets or sets the originating project part identifier, when known.</summary>
        public Guid? SourceProjectPartId { get; set; }

        /// <summary>Gets or sets the material identifier used by production.</summary>
        public required Guid MaterialId { get; set; }

        /// <summary>Gets or sets the locked material snapshot JSON used by production.</summary>
        public required string MaterialSnapshotJson { get; set; }

        /// <summary>Gets or sets the locked configuration snapshot JSON used by production.</summary>
        public required string ConfigurationSnapshotJson { get; set; }

        /// <summary>Gets or sets the manufacturing technology.</summary>
        public required string Technology { get; set; }

        /// <summary>Gets or sets the part volume in cubic centimeters.</summary>
        public decimal VolumeCm3 { get; set; }

        /// <summary>Gets or sets the ordered quantity.</summary>
        public int Quantity { get; set; }

        /// <summary>Gets or sets the estimated print time in minutes.</summary>
        public int EstimatedPrintTimeMinutes { get; set; }

        /// <summary>Gets or sets the promised delivery date.</summary>
        public DateTime? DeliveryDate { get; set; }
    }
}
